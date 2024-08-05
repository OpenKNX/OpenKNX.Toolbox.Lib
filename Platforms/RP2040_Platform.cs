using OpenKNX.Toolbox.Lib.Data;

namespace OpenKNX.Toolbox.Lib.Platforms;

public class RP2040_Platform : IPlatform
{
    public ArchitectureType Architecture { get; } = ArchitectureType.RP2040;

    public List<PlatformDevice> GetDevices()
    {
        List<PlatformDevice> devices = new();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType == DriveType.Removable &&
                drive.VolumeLabel == "RPI-RP2")
                devices.Add(new PlatformDevice(Architecture, drive.VolumeLabel, drive.Name, "copy"));
        }

        devices.Add(new PlatformDevice(Architecture, "Test Drive", "K:\\", "copy"));

        return devices;
    }

    public async Task DoUpload(PlatformDevice device, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null)
    {
        switch(device.Method)
        {
            case "copy":
                UploadViaCopy(device.Path, firmwarePath, progress);
                break;

            default:
                throw new NotImplementedException(device.Method);
        }
    }

    public async Task UploadViaCopy(string drive, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null)
    {
        FileStream source = new FileStream(firmwarePath, System.IO.FileMode.Open);
        FileStream target = new FileStream(Path.Combine(drive, "firmware.uf2"), System.IO.FileMode.Create);

        byte[] buffer = new byte[1024];
        int readedBytes = 0;
        int wroteBytes = 0;
        while(true) {
            readedBytes = source.Read(buffer, 0, 1024);
            target.Write(buffer, 0, readedBytes);
            wroteBytes += readedBytes;
            progress?.Report(new KeyValuePair<long, long>(wroteBytes, source.Length));
            if(readedBytes < 1024)
                break;
        }
        await target.FlushAsync();
        target.Close();
        source.Close();
    }
}