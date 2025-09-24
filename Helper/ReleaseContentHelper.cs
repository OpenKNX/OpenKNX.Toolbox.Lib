using System.Text.RegularExpressions;
using OpenKNX.Toolbox.Lib.Data;
using OpenKNX.Toolbox.Lib.Exceptions;
using OpenKNX.Toolbox.Lib.Models;
using System.Xml.Linq;
using System.Management.Automation;

namespace OpenKNX.Toolbox.Lib.Helper;


public static class ReleaseContentHelper
{
    public static ReleaseContentModel GetReleaseContent(string path, SemanticVersion version)
    {
        string contentPath = System.IO.Path.Combine(path, "content.xml");
        if(!System.IO.File.Exists(contentPath))
            contentPath = System.IO.Path.Combine(path, "data", "content.xml");
        if(!System.IO.File.Exists(contentPath))
            throw new FileNotFoundException("content.xml not found in the specified path.", contentPath);
        CheckContentXml(contentPath);
        XElement xele = XElement.Load(contentPath);
        return ParseReleaseContent(xele, Path.GetDirectoryName(contentPath) ?? path, version);
    }

    private static void CheckContentXml(string path)
    {
        string content = System.IO.File.ReadAllText(path);
        content = content.Replace("    <Products>\r\n</Content>", "    </Products>\r\n</Content>");
        System.IO.File.WriteAllText(path, content);
    }
    
    private static string GetAbsolutePath(string basePath, string? relativePath)
    {
        return Path.GetFullPath(Path.Combine(basePath, relativePath ?? "not-found"));
    }

    private static ReleaseContentModel ParseReleaseContent(XElement root, string path, SemanticVersion version)
    {
        ReleaseContentModel model = new();

        string appId = "";
        foreach(string part in path.Split('\\'))
        {
            if(part.StartsWith('$'))
            {
                appId = part;
                break;
            }
        }

        XElement? etsApp = root.Element("ETSapp");
        if (etsApp == null) throw new ElementNotFoundException("ETSapp");
        if (etsApp.Attribute("XmlFile") == null) throw new AttributeNotFoundException("XmlFile");

        model.XmlFile = GetAbsolutePath(path, etsApp.Attribute("XmlFile")?.Value);
        model.ReleaseName = etsApp.Attribute("Name")?.Value ?? "Unbenannt";

        XElement? prods = root.Element("Products");
        if (prods == null) throw new ElementNotFoundException("Products");
        foreach (XElement prod in prods.Elements())
        {
            Product product = new(prod.Attribute("Name")?.Value ?? "Unbenannt",
                GetAbsolutePath(path, prod.Attribute("Firmware")?.Value),
                appId,
                version);

            if (prod.Attribute("Processor") == null) throw new AttributeNotFoundException("Processor");
            object? type;
            if (!Enum.TryParse(typeof(ArchitectureType), prod.Attribute("Processor")?.Value, out type))
                throw new Exception($"Unbekannte Architektur: {prod.Attribute("Processor")?.Value}");
            product.Architecture = (ArchitectureType)type;
            model.Products.Add(product);
        }

        return model;
    }
}