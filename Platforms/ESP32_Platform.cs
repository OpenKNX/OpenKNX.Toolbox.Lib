using EspDotNet;
using EspDotNet.Communication;
using EspDotNet.Config;
using EspDotNet.Loaders;
using EspDotNet.Loaders.SoftLoader;
using EspDotNet.Tools;
using EspDotNet.Tools.Firmware;
using OpenKNX.Toolbox.Lib.Data;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenKNX.Toolbox.Lib.Platforms;

public class ESP32_Platform : IPlatform
{
    private static List<string> ProductList = new()
    {
        "VID_1A86&PID_7523"
    };

    enum OtaCommands
    {
        Flash = 0,
        SPIFFS = 100,
        AUTH = 200
    }

    public ArchitectureType Architecture { get; } = ArchitectureType.ESP32;
    
    public async Task GetDevices(ObservableCollection<PlatformDevice> devices, bool onlySerial = false)
    {
        await FindEsp32(devices);
    }

    private async Task FindEsp32(ObservableCollection<PlatformDevice> devices)
    {
        if (OperatingSystem.IsWindows())
        {
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Ports'");
            foreach (ManagementObject device in searcher.Get())
            {
                if (device["Status"]?.ToString() != "OK")
                    continue;
                string deviceId = device["DeviceID"]?.ToString() ?? "";
                Regex regex = new Regex(@"USB\\(VID_[A-F0-9a-f]{4}&PID_[A-F0-9a-f]{4})");
                Match m = regex.Match(deviceId);
                if(!m.Success)
                    continue;
                string deviceVendor = m.Groups[1].Value;
                if (!ProductList.Contains(deviceVendor))
                    continue;
                string deviceName = device["Name"]?.ToString() ?? "";
                regex = new Regex(@"(COM\d{1,3})");
                m = regex.Match(deviceName);
                if(!m.Success)
                    continue;
                string deviceCOM = m.Groups[1].Value;
                devices.Add(new PlatformDevice(Architecture, "ESP32 (Boot)", deviceCOM, "tool"));
            }
        }
    }

    public async Task DoUpload(PlatformDevice device, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null)
    {
        switch(device.Method)
        {
            case "tool":
                await UploadTool(device, firmwarePath, progress);
                break;

            case "ota":
                await UploadOTA(device, firmwarePath, progress);
                break;

            default:
                throw new Exception("Not supported Mehotd for ESP: " + device.Method);
        }
        
    }

    private async Task UploadTool(PlatformDevice device, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null)
    {
        ESPToolbox toolbox = new ESPToolbox();

        Communicator comm = toolbox.CreateCommunicator();
        Debug.WriteLine("Open Serial " + device.Path + " @115200");
        toolbox.OpenSerial(comm, device.Path, 115200);

        Debug.WriteLine("Entering Bootloader...");
        ILoader loader = await toolbox.StartBootloaderAsync(comm);

        ChipTypes type = await toolbox.DetectChipTypeAsync(loader);
        Debug.WriteLine("Detected Chip: " + type.ToString());

        loader = await toolbox.StartSoftloaderAsync(comm, loader, type);
        Debug.WriteLine("Started Softloader");

        string factoryBin = firmwarePath;
        if (!factoryBin.EndsWith(".factory.bin"))
            factoryBin = factoryBin.Replace(".bin", ".factory.bin");

        IUploadTool uploader = toolbox.CreateUploadFlashTool(loader, type);
        Debug.WriteLine("Uploading Firmware: " + factoryBin);

        var firmware = GetFirmware(factoryBin);
        var progress_float = new Progress<float>(p =>
        {
            progress?.Report(new KeyValuePair<long, long>((long)(p * 100), 100));
        });

        CancellationTokenSource cts = new CancellationTokenSource();
        await toolbox.ChangeBaudAsync(comm, loader, 921600);
        await toolbox.UploadFirmwareAsync(uploader, firmware, cts.Token, progress_float);
        await toolbox.ResetDeviceAsync(comm);
    }

    private async Task UploadOTA(PlatformDevice device, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null)
    {
        //if (!firmwarePath.EndsWith(".factory.bin"))
        //    firmwarePath = firmwarePath.Replace(".bin", ".factory.bin");

        string ipAddress = device.Path.Split(':')[0];
        int ipPort = int.Parse(device.Path.Split(':')[1]);
        long fileSize = new FileInfo(firmwarePath).Length;
        string fileMD5 = GetFileMd5(firmwarePath);

        TcpListener tcp = new TcpListener(IPAddress.Any, 43232);
        tcp.Start();

        int tcpPort = tcp.LocalEndpoint is IPEndPoint ep ? ep.Port : throw new Exception("Unable to get local TCP port.");

        UdpClient udp = new(AddressFamily.InterNetwork);
        byte[] msgBytes = Encoding.UTF8.GetBytes($"{(int)OtaCommands.Flash} {tcpPort} {fileSize} {fileMD5}");

        int retries = 0;
        while (retries < 4)
        {
            try
            {
                Debug.WriteLine($"Send invitation to {ipAddress}:{ipPort} [{retries}");
                await udp.SendAsync(msgBytes, msgBytes.Length, ipAddress, ipPort);
                CancellationTokenSource ctsi = new CancellationTokenSource(2000);
                UdpReceiveResult result = await udp.ReceiveAsync(ctsi.Token);
                string response = Encoding.UTF8.GetString(result.Buffer);
                if (response == "AUTH")
                    throw new Exception("Device requires authentication, which is not supported.");
                if (response != "OK")
                    throw new Exception($"Device did not respond with OK. [{response}");
                Debug.WriteLine("Device responded with OK.");
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error while sending UDP packet: " + ex.Message);
                retries++;
                continue;
            }
        }

        if(retries == 4)
            throw new Exception("No response from device.");




        Debug.WriteLine("Waiting for device to connect...");
        CancellationTokenSource cts = new CancellationTokenSource(10000);
        Socket remote = await tcp.AcceptSocketAsync(cts.Token);
        Debug.WriteLine("Received connection from device: " + remote.LocalEndPoint?.ToString() ?? "unbekannt");

        using FileStream fs = new(firmwarePath, FileMode.Open, FileAccess.Read);

        int bufferSize = 1024;
        while (fs.Position < fs.Length)
        {
            byte[] buffer = new byte[bufferSize];
            int readed = await fs.ReadAsync(buffer, 0, buffer.Length);
            if (readed == bufferSize)
                await remote.SendAsync(buffer);
            else
                // if not full buffer, send only the readed bytes
                await remote.SendAsync(buffer.Take(readed).ToArray());

            CancellationTokenSource ctsr = new CancellationTokenSource(1000);
            int received = await remote.ReceiveAsync(buffer, ctsr.Token);
            string response = Encoding.UTF8.GetString(buffer, 0, received);
            int written = 0;
            if (!int.TryParse(response, out written))
                throw new Exception("Responde is not an integer: " + response);
            if (written != readed)
                throw new Exception("Das Gerät hat weniger geschriebene Bytes zurückgemeldet, als gesendet wurden");

            progress?.Report(new KeyValuePair<long, long>(fs.Position, fs.Length));
        }
    }

    public static string GetFileMd5(string filePath)
    {
        using (var md5 = MD5.Create())
        using (var stream = File.OpenRead(filePath))
        {
            byte[] hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }


    private IFirmwareProvider GetFirmware(string pathToFirmware) // this is the local path of the merged-firmware.bin file
    {
        return new FirmwareProvider(
            entryPoint: 0x00000000,
            segments:
            [
                new FirmwareSegmentProvider(0x00000000, File.ReadAllBytes(pathToFirmware)),
            ]
        );
    }
}