

using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace OpenKNX.Toolbox.Lib.Data;

public class Repository
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    [JsonIgnore]
    public ObservableCollection<Release> Releases { get; set; } = new();
    public List<Release> ReleasesAll { get; set; } = new();

    public override string ToString()
    {
        return Name;
    }

    public Repository(string name, string url)
    {
        Name = name;
        Url = url;
    }
}