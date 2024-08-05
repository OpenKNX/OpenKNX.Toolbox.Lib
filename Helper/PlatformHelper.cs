using OpenKNX.Toolbox.Lib.Platforms;

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

    public static List<PlatformDevice> GetDevices()
    {
        List<PlatformDevice> devices = new();

        foreach(IPlatform platform in GetPlatforms())
        {
            devices.AddRange(platform.GetDevices());
        }

        return devices;
    }
}