using OpenKNX.Toolbox.Lib.Data;

namespace OpenKNX.Toolbox.Lib.Platforms;

public class PlatformDevice
{
    public ArchitectureType Architecture { get; }
    public string Name { get; }
    public string Path { get; }
    public string Method { get; }

    public PlatformDevice(ArchitectureType architecture, string name, string path, string method)
    {
        Architecture = architecture;
        Name = name;
        Path = path;
        Method = method;
    }
    
    public override string ToString()
    {
        return $"{Architecture} {Name} {Path}";
    }

}