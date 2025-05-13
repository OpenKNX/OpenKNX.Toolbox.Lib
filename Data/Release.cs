

namespace OpenKNX.Toolbox.Lib.Data;

public class Release : IComparable
{

    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string UrlRelease { get; set; } = "";
    public bool IsPrerelease { get; set; } = false;
    public DateTimeOffset? Published { get; set; }

    public int Major { get; set; } = 0;
    public int Minor { get; set; } = 0;
    public int Build { get; set; } = 0;

    public override string ToString()
    {
        return $"v{Major}.{Minor}.{Build} - {Name}{(IsPrerelease ? " (PreRelease)" : "")}";
    }

    public int CompareTo(object? obj)
    {
        Release? rel = obj as Release;
        if(rel == null) return 0;

        if(Major == rel.Major)
        {
            if(Minor == rel.Minor)
            {
                if(Build == rel.Build)
                    return Name.CompareTo(rel.Name);
                return Build.CompareTo(rel.Build)*-1;
            }
            return Minor.CompareTo(rel.Minor)*-1;
        }
        return Major.CompareTo(rel.Major)*-1;
    }
}