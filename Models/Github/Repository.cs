

namespace OpenKNX.Toolbox.Lib.Models.Github;

using Newtonsoft.Json;

public class Repository
{
    [JsonProperty("repo_url")]
    public string Url { get; set; } = "";
    
    [JsonProperty("description")]
    public string Description { get; set; } = "";

    [JsonProperty("archived")]
    public bool IsArchived { get; set; } = false;

    [JsonProperty("releases")]
    public List<Release> Releases { get; set; } = new();
}