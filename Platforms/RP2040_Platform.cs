using OpenKNX.Toolbox.Lib.Data;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.InteropServices;

namespace OpenKNX.Toolbox.Lib.Platforms;

public class RP2040_Platform : IPlatform
{
    public ArchitectureType Architecture { get; } = ArchitectureType.RP2040;

    public async Task<List<PlatformDevice>> GetDevices()
    {
        List<PlatformDevice> devices = new();

        FindUsbDrives(devices);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            await FindPicoWindows(devices);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            await FindPicoLinux(devices);

#if DEBUG
        devices.Add(new PlatformDevice(Architecture, "Test Drive", "K:\\", "copy"));
#endif

        return devices;
    }

    private void FindUsbDrives(List<PlatformDevice> devices)
    {
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady && drive.DriveType == DriveType.Removable &&
                drive.VolumeLabel == "RPI-RP2")
                devices.Add(new PlatformDevice(Architecture, drive.VolumeLabel, drive.Name, "copy"));
        }
    }

    private async Task FindPicoWindows(List<PlatformDevice> devices)
    {
        string command = "$portList = get-pnpdevice -class Ports\r\n" +
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
        using var proc = new Process { StartInfo = { 
            UseShellExecute = false, 
            FileName = "powershell.exe",
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            Arguments = command
        } };
        proc.Start();
        List<string> lines = new();
        await proc.WaitForExitAsync();
        while (!proc.StandardOutput.EndOfStream)
        {
            string line = await proc.StandardOutput.ReadLineAsync();
            lines.Add(line);
        }
            
        foreach(string port in lines)
            devices.Add(new PlatformDevice(Architecture, "RPI-RP2 (Boot)", port, "boot"));
    }

    private async Task FindPicoLinux(List<PlatformDevice> devices)
    {
        throw new Exception("Diese Funktion ist auf Linux nicht verfügbar");

        /*
flu0@laptop:~$ ls /dev/serial/by-path/
total 0
lrwxrwxrwx 1 root root 13 2011-07-20 17:12 pci-0000:00:0b.0-usb-0:3:1.0-port0 -> ../../ttyUSB0
        */
    }

    public async Task DoUpload(PlatformDevice device, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null)
    {
        switch(device.Method)
        {
            case "copy":
                await UploadViaCopy(device.Path, firmwarePath, progress);
                break;

            case "boot":
                await UploadViaBoot(device.Path, firmwarePath, progress);
                break;

            default:
                throw new NotImplementedException(device.Method);
        }
    }

    public async Task UploadViaCopy(string drive, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null)
    {
        Console.WriteLine("opening files");
        FileStream source = new FileStream(firmwarePath, System.IO.FileMode.Open);
        FileStream target = new FileStream(Path.Combine(drive, "firmware.uf2"), System.IO.FileMode.Create);
        Console.WriteLine("opening finished");

        const int bufferSize = 4096;
        byte[] buffer = new byte[bufferSize];
        int readedBytes = 0;
        int wroteBytes = 0;
        while(true) {
            readedBytes = await source.ReadAsync(buffer, 0, bufferSize);
            await target.WriteAsync(buffer, 0, readedBytes);
            wroteBytes += readedBytes;
            //progress?.Report(new KeyValuePair<long, long>(wroteBytes, source.Length));
            // if(wroteBytes % 51200 == 0)
            //     await Task.Delay(1);
            if(readedBytes < bufferSize)
                break;
        }
        await target.FlushAsync();
        target.Close();
        source.Close();
    }

    public async Task UploadViaBoot(string port, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null)
    {
        List<PlatformDevice> devices = new();
        FindUsbDrives(devices); //get usb drives before we started, so we know which ist the correct one

        SerialPort sp = new SerialPort(port, 115200);
        Exception exception = new Exception("");
        try {
            sp.Open();
            sp.Write(new byte[] { 7 }, 0, 1); //tell device to save data
            await Task.Delay(1000);
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

        await Task.Delay(1000);

        List<PlatformDevice> new_devices = new();
        FindUsbDrives(new_devices);
        PlatformDevice? new_device = null;
        foreach(PlatformDevice device in new_devices)
            if(!devices.Any(d => d.Path == device.Path))
                new_device = device;
        
        if(new_device == null)
            throw new Exception("Gerät konnte nicht in den Bootloadermodus versetzt werden.", exception);

        UploadViaCopy(new_device.Path, firmwarePath, progress);
    }
}