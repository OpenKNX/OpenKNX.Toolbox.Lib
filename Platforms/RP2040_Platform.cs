using OpenKNX.Toolbox.Lib.Data;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Management;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace OpenKNX.Toolbox.Lib.Platforms;

public class RP2040_Platform : IPlatform
{
    public ArchitectureType Architecture { get; } = ArchitectureType.RP2040;

    public async Task GetDevices(ObservableCollection<PlatformDevice> devices, bool onlySerial = false)
    {
        if(!onlySerial)
            FindUsbDrives(devices);

        await FindPicoWindows(devices);

#if DEBUG
        if(!onlySerial)
            devices.Add(new PlatformDevice(Architecture, "Test Drive", @"C:\Users\Mike\Desktop\", "copy"));
#endif
    }

    private void FindUsbDrives(ObservableCollection<PlatformDevice> devices)
    {
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady && drive.DriveType == DriveType.Removable &&
                drive.VolumeLabel == "RPI-RP2")
                devices.Add(new PlatformDevice(Architecture, drive.VolumeLabel, drive.Name, "copy"));
        }
    }

    private async Task FindPicoWindows(ObservableCollection<PlatformDevice> devices)
    {
        if(OperatingSystem.IsWindows())
        {
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Ports'");
            foreach (ManagementObject device in searcher.Get())
            {
                if (device["Status"]?.ToString() != "OK")
                    continue;
                string deviceId = device["DeviceID"]?.ToString() ?? "";
                if (!deviceId.Contains("USB\\VID_2E8A"))
                    continue;
                string deviceName = device["Name"]?.ToString() ?? "";
                if (!deviceName.Contains("COM"))
                    continue;
                Regex regex = new Regex(@"(COM\d{1,3})");
                string deviceCOM = regex.Match(deviceName).Groups[1].Value;
                devices.Add(new PlatformDevice(Architecture, "RPI-RP2 (Boot)", deviceCOM, "boot"));
            }
        } else
        {
            string command = "$portList = get-pnpdevice -class Ports -erroraction 'silentlycontinue'\r\n" +
                    "foreach ($usbDevice in $portList) {\r\n" +
                        "\tif ($usbDevice.Present) {\r\n" +
                            "\t\t$isPico = $usbDevice.InstanceId.StartsWith('USB\\VID_2E8A')\r\n" +
                            "\t\t$isCom = $usbDevice.Name -match 'COM\\d{1,3}'\r\n" +
                            "\t\tif ($isPico) {\r\n" +
                                "\t\t\t$port = $Matches[0]\r\n" +
                                "\t\t\tWrite-Host \"$port\"\r\n" +
                            "\t\t}\r\n" +
                        "\t}\r\n" +
                    "}";
            using var proc = new Process
            {
                StartInfo = {
                    UseShellExecute = false,
                    FileName = "powershell.exe",
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    Arguments = command
                }
            };
            proc.Start();
            List<string> lines = new();
            await proc.WaitForExitAsync();
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = await proc.StandardOutput.ReadLineAsync() ?? string.Empty;
                if(!string.IsNullOrEmpty(line))
                    lines.Add(line);
            }

            foreach (string port in lines)
                devices.Add(new PlatformDevice(Architecture, "RPI-RP2 (Boot)", port, "boot"));
        }
    }

    public async Task DoUpload(PlatformDevice device, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null, CancellationToken token = default)
    {
        switch(device.Method)
        {
            case "copy":
                await UploadViaCopy(device.Path, firmwarePath, progress, token);
                break;

            case "boot":
                await UploadViaBoot(device.Path, firmwarePath, progress, token);
                break;

            case "ota":
                ESP32_Platform dummy = new ESP32_Platform();
                await dummy.DoUpload(device, firmwarePath, progress, token);
                return;

            default:
                throw new NotImplementedException(device.Method);
        }
    }

    public async Task UploadViaCopy(string drive, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null, CancellationToken token = default)
    {
        FileStream source = new FileStream(firmwarePath, FileMode.Open);
        FileStream target = new FileStream(Path.Combine(drive, "firmware.uf2"), FileMode.Create);

        const int bufferSize = 4096;
        byte[] buffer = new byte[bufferSize];
        int readedBytes = 0;
        int wroteBytes = 0;

        while (!token.IsCancellationRequested) {
            readedBytes = await source.ReadAsync(buffer, 0, bufferSize, token);
            await target.WriteAsync(buffer, 0, readedBytes, token);
            wroteBytes += readedBytes;
            progress?.Report(new KeyValuePair<long, long>(wroteBytes, source.Length));
            if(readedBytes < bufferSize)
                break;
        }
        await target.FlushAsync();
        target.Close();
        source.Close();
    }

    public async Task UploadViaBoot(string port, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null, CancellationToken token = default)
    {
        ObservableCollection<PlatformDevice> devices = new();
        FindUsbDrives(devices); //get usb drives before we started, so we know which ist the correct one

        SerialPort sp = new SerialPort(port, 115200);
        Exception exception = new Exception("");
        try {
            sp.Open();
            sp.Write(new byte[] { 7 }, 0, 1); //tell device to save data
            await Task.Delay(1000, token);
        } catch(Exception ex) {
            exception = ex;
            Console.WriteLine("RPI-RP2: Fehler beim Anweisen Daten zu speichern");
            Console.WriteLine(ex.Message);
        }
        if(sp.IsOpen)
            sp.Close();

        sp = new SerialPort(port, 1200);
        try {
            sp.Open();
        } catch {
            // its okay to get an exception here
        }
        if(sp.IsOpen)
            sp.Close();

        await Task.Delay(1000, token);

        if(token.IsCancellationRequested)
            return;

        ObservableCollection<PlatformDevice> new_devices = new();
        FindUsbDrives(new_devices);
        PlatformDevice? new_device = null;
        foreach(PlatformDevice device in new_devices)
            if(!devices.Any(d => d.Path == device.Path))
                new_device = device;
        
        if(new_device == null)
            throw new Exception("Gerät konnte nicht in den Bootloadermodus versetzt werden.", exception);

        if (token.IsCancellationRequested)
            return;

        await UploadViaCopy(new_device.Path, firmwarePath, progress, token);
    }
}
