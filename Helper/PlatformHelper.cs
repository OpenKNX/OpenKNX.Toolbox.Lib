using OpenKNX.Toolbox.Lib.Data;
using OpenKNX.Toolbox.Lib.Platforms;
using System.Collections.ObjectModel;

namespace OpenKNX.Toolbox.Lib.Helper;

public class PlatformHelper
{
    public static List<IPlatform> GetPlatforms()
    {
        return new List<IPlatform>() {
            new ESP32_Platform(),
            new RP2040_Platform()
        };
    }

    public static async Task GetDevices(ObservableCollection<PlatformDevice> devices)
    {
        foreach (IPlatform platform in GetPlatforms())
        {
            await platform.GetDevices(devices);
        }
    }

    public static async Task GetDevices(ObservableCollection<PlatformDevice> devices, ArchitectureType arch)
    {
        foreach (IPlatform platform in GetPlatforms())
        {
            if(platform.Architecture == arch)
                await platform.GetDevices(devices);
        }
    }

    public static async Task DoUpload(PlatformDevice device, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null)
    {
        IPlatform? platform = GetPlatforms().FirstOrDefault(p => p.Architecture == device.Architecture);
        if(platform == null)
            throw new Exception("No platform found for this device.");
        await platform.DoUpload(device, firmwarePath, progress);
    }
}