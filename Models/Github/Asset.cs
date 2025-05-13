using Newtonsoft.Json;

namespace OpenKNX.Toolbox.Lib.Models.Github;

public class Asset
{
    [JsonProperty("name")]
    public string Name { get; set; } = "";
    
    [JsonProperty("browser_download_url")]
    public string Url { get; set; } = "";
    
    [JsonProperty("updated_at")]
    public DateTime Updated { get; set; } = DateTime.MinValue;
}