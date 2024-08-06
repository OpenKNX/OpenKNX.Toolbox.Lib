using OpenKNX.Toolbox.Lib.Data;

namespace OpenKNX.Toolbox.Lib.Platforms;

public interface IPlatform
{
    public ArchitectureType Architecture { get; }
    public Task<List<PlatformDevice>> GetDevices();
    public Task DoUpload(PlatformDevice device, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null);
}