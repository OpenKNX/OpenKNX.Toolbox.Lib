

using System.Management.Automation;
using Newtonsoft.Json;

namespace OpenKNX.Toolbox.Lib.Data;

public class Release : IComparable
{

    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string UrlRelease { get; set; } = "";
    public bool IsPrerelease { get; set; } = false;
    public DateTimeOffset? Published { get; set; }

    [JsonIgnore]
    public SemanticVersion Version { get; set; } = new SemanticVersion(0, 0, 0);

    private string _versionString = "";
    public string VersionString
    {
        get
        {
            if (Version.ToString() != "0.0.0")
                return Version.ToString();
            return _versionString;
        }

        set
        {
            _versionString = value;
            try
            {
                Version = new SemanticVersion(value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing version: {ex.Message}");
            }
        }
    }

    public override string ToString()
    {
        return $"v{Version}{(IsPrerelease ? " (PreRelease)" : "")}";
    }

    public int CompareTo(object? obj)
    {
        Release? rel = obj as Release;
        if(rel == null) return 0;

        if(Version.Major == rel.Version.Major)
        {
            if (Version.Minor == rel.Version.Minor)
            {
                if (Version.Patch == rel.Version.Patch)
                {
                    if (Version.BuildLabel == rel.Version.BuildLabel)
                        return Name.CompareTo(rel.Name);
                    return Version.BuildLabel.CompareTo(rel.Version.BuildLabel) * -1;
                }
                return Version.Patch.CompareTo(rel.Version.Patch)*-1;
            }
            return Version.Minor.CompareTo(rel.Version.Minor)*-1;
        }
        return Version.Major.CompareTo(rel.Version.Major)*-1;
    }
}