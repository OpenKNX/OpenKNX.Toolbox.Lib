using OpenKNX.Toolbox.Lib.Models;
using System.Management.Automation;

namespace OpenKNX.Toolbox.Lib.Data;

public class Product
{
    public string Name { get; } = "";
    public string FirmwareFile { get; } = "";
    public string AppId { get; } = "";
    public ArchitectureType Architecture { get; set; } = ArchitectureType.RP2040;
    public SemanticVersion Version { get; }

    public Product(string name, string file, string appId, SemanticVersion version)
    {
        Name = name;
        FirmwareFile = file;
        AppId = appId;
        Version = version;
    }

    public override string ToString()
    {
        return $"{Architecture} {Name}";
    }
}