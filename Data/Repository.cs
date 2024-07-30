

namespace OpenKNX.Toolbox.Lib.Data;

public class Repository
{
    public long Id { get; set; } = 0;
    public string Name { get; set; } = "";
    public List<Release> Releases { get; set; } = new();

    public override string ToString()
    {
        return Name;
    }
}