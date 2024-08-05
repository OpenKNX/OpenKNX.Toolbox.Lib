using OpenKNX.Toolbox.Lib.Data;

namespace OpenKNX.Toolbox.Lib.Platforms;

public class ESP32_Platform : IPlatform
{
    public ArchitectureType Architecture { get; } = ArchitectureType.ESP32;
    
    public List<PlatformDevice> GetDevices()
    {
        List<PlatformDevice> devices = new();

        return devices;
    }

    public async Task DoUpload(PlatformDevice device, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null)
    {
        throw new NotImplementedException();
    }
}