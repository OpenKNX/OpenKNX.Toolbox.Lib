using Newtonsoft.Json;

namespace OpenKNX.Toolbox.Lib.Models.Github;

public class OpenKnxContent
{
    public string OpenKnxContentType { get; set; } = "";
    public string OpenKnxFormatVersion { get; set; } = "";

    [JsonProperty("data")]
    public Dictionary<string, Repository> Repositories { get; set; } = new();

    public System.Management.Automation.SemanticVersion GetSemanticVersion()
    {
        string version = OpenKnxFormatVersion;
        if(version.ToLower().StartsWith("v"))
            version = version.Substring(1);
        return new System.Management.Automation.SemanticVersion(version);
    }
}