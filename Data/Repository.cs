

using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace OpenKNX.Toolbox.Lib.Data;

public class Repository
{
    public long Id { get; set; } = 0;
    public string Name { get; set; } = "";
    [JsonIgnore]
    public ObservableCollection<Release> Releases { get; set; } = new();
    public List<Release> ReleasesAll { get; set; } = new();

    public override string ToString()
    {
        return Name;
    }
}