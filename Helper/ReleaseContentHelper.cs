using System.Text.RegularExpressions;
using OpenKNX.Toolbox.Lib.Data;
using OpenKNX.Toolbox.Lib.Exceptions;
using OpenKNX.Toolbox.Lib.Models;
using System.Xml.Linq;

namespace OpenKNX.Toolbox.Lib.Helper;


public static class ReleaseContentHelper
{
    public static ReleaseContentModel GetReleaseContent(string path)
    {
        string contentPath = System.IO.Path.Combine(path, "content.xml");
        if(!System.IO.File.Exists(contentPath))
            contentPath = System.IO.Path.Combine(path, "data", "content.xml");
        if(!System.IO.File.Exists(contentPath))
            throw new FileNotFoundException("content.xml not found in the specified path.", contentPath);
        CheckContentXml(contentPath);
        XElement xele = XElement.Load(contentPath);
        return ParseReleaseContent(xele, Path.GetDirectoryName(contentPath) ?? path);
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

    private static ReleaseContentModel ParseReleaseContent(XElement root, string path)
    {
        ReleaseContentModel model = new();

        XElement? etsApp = root.Element("ETSapp");
        if (etsApp == null) throw new ElementNotFoundException("ETSapp");
        if (etsApp.Attribute("XmlFile") == null) throw new AttributeNotFoundException("XmlFile");

        model.XmlFile = GetAbsolutePath(path, etsApp.Attribute("XmlFile")?.Value);

        XElement? prods = root.Element("Products");
        if (prods == null) throw new ElementNotFoundException("Products");
        foreach (XElement prod in prods.Elements())
        {
            Product product = new()
            {
                Name = prod.Attribute("Name")?.Value ?? "Unbenannt",
                FirmwareFile = GetAbsolutePath(path, prod.Attribute("Firmware")?.Value),
                ReleaseContent = model
            };
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