using System.Diagnostics;
using System.Runtime.InteropServices;
using ESPTool.Devices;
using OpenKNX.Toolbox.Lib.Data;

namespace OpenKNX.Toolbox.Lib.Platforms;

public class ESP32_Platform : IPlatform
{
    public ArchitectureType Architecture { get; } = ArchitectureType.ESP32;
    
    public async Task<List<PlatformDevice>> GetDevices()
    {
        List<PlatformDevice> devices = new();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            await FindEsp32Windows(devices);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            await FindEsp32Linux(devices);

        return devices;
    }

    private async Task FindEsp32Windows(List<PlatformDevice> devices)
    {
        string command = "$portList = get-pnpdevice -class Ports -erroraction 'silentlycontinue'\r\n" +
                    "foreach ($usbDevice in $portList) {\r\n" + 
                        "\tif ($usbDevice.Present) {\r\n" +
                            "\t\t$isPico = $usbDevice.InstanceId.StartsWith('USB\\VID_10C4')\r\n" +
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
            devices.Add(new PlatformDevice(Architecture, "(Boot)", port, "boot"));
    }

    private async Task FindEsp32Linux(List<PlatformDevice> devices)
    {
        
    }

    public async Task DoUpload(PlatformDevice device, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null)
    {
        Device dev = new Device();
        Console.WriteLine("Open Device " + device.Path);
        ESPTool.Result result = await dev.OpenSerial(device.Path, 115200);
        if(!result.Success)
            throw new Exception(result.Error.ToString());
        Console.Write("Entering Bootloader...  ");
        result = await dev.EnterBootloader();
        Console.WriteLine(result.Success ? "OKAY" : "FAIL");
        Console.Write("Syncing...  ");
        result = await dev.Sync();
        Console.WriteLine(result.Success ? "OKAY" : "FAIL");
        Console.Write("Checking Type... ");
        ESPTool.Result<ChipTypes> type = await dev.DetectChipType();
        Console.WriteLine(type.Success ? type.ToString() : "FAIL");
    }
}