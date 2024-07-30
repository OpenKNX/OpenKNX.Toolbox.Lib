using OpenKNX.Toolbox.Lib.Models;

namespace OpenKNX.Toolbox.Lib.Data;

public class Product
{
    public string Name { get; set; } = "";
    public string FirmwareFile { get; set; } = "";
    public ArchitectureType Architecture { get; set; } = ArchitectureType.RP2040;

    public Product() { }

    public Product(string name, string file, ArchitectureType arch)
    {
        Name = name;
        FirmwareFile = file;
        Architecture = arch;
    }

    [Newtonsoft.Json.JsonIgnore]
    public ReleaseContentModel? ReleaseContent { get; set; }

    public override string ToString()
    {
        return Name;
    }
}