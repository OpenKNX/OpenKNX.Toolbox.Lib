using Newtonsoft.Json;

namespace OpenKNX.Toolbox.Lib.Models.Github;

public class Release
{
    [JsonProperty("tag_name")]
    public string Tag { get; set; } = "";
    
    [JsonProperty("name")]
    public string Name { get; set; } = "";
    
    [JsonProperty("html_url")]
    public string Url { get; set; } = "";
    
    // [JsonProperty("body")]
    // public string Body { get; set; } = "";
    
    [JsonProperty("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonProperty("prerelease")]
    public bool IsPrerelease { get; set; } = false;
    
    [JsonProperty("assets")]
    public List<Asset> Assets { get; set; } = new();
}