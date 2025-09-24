using OpenKNX.Toolbox.Lib.Data;
using System.Collections.ObjectModel;

namespace OpenKNX.Toolbox.Lib.Platforms;

public interface IPlatform
{
    public ArchitectureType Architecture { get; }
    public Task GetDevices(ObservableCollection<PlatformDevice> devices, bool onlySerial = false);
    public Task DoUpload(PlatformDevice device, string firmwarePath, IProgress<KeyValuePair<long, long>>? progress = null);
}