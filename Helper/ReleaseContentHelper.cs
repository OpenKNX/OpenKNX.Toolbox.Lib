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
        CheckContentXml(System.IO.Path.Combine(path, "content.xml"));
        XElement xele = XElement.Load(System.IO.Path.Combine(path, "content.xml"));
        return ParseReleaseContent(xele, path);
    }

    private static void CheckContentXml(string path)
    {
        string content = System.IO.File.ReadAllText(path);
        content = content.Replace("    <Products>\r\n</Content>", "    </Products>\r\n</Content>");
        System.IO.File.WriteAllText(path, content);
    }

    private static ReleaseContentModel ParseReleaseContent(XElement root, string path)
    {
        ReleaseContentModel model = new ();

        XElement? etsApp = root.Element("ETSapp");
        if(etsApp == null) throw new ElementNotFoundException("ETSapp");
        if(etsApp.Attribute("XmlFile") == null) throw new AttributeNotFoundException("XmlFile");
        model.XmlFile = System.IO.Path.Combine(path, etsApp.Attribute("XmlFile")?.Value);

        XElement? prods = root.Element("Products");
        if(prods == null) throw new ElementNotFoundException("Products");
        foreach(XElement prod in prods.Elements())
        {
            Product product = new() {
                Name = prod.Attribute("Name")?.Value ?? "Unbenannt",
                FirmwareFile = System.IO.Path.Combine(path, prod.Attribute("Firmware")?.Value)
            };
            if(prod.Attribute("Processor") == null) throw new AttributeNotFoundException("Processor");
            object? type;
            if(!Enum.TryParse(typeof(ArchitectureType), prod.Attribute("Processor")?.Value, out type))
                throw new Exception($"Unbekannte Architektur: {prod.Attribute("Processor")?.Value}");
            product.Architecture = (ArchitectureType)type;
            model.Products.Add(product);
        }

        return model;
    }

    /// <summary>
    /// Reads the "content.xml" file from a release.
    /// </summary>
    /// <param name="releaseDirectory">The release directory containing a "data\content.xml" file.</param>
    /// <returns>Returns a "ReleaseContent" object.</returns>
//     public static ReleaseContent GetReleaseContent(string releaseDirectory)
//     {
//         var contentXmlPath = Path.Combine(releaseDirectory, "data", "content.xml");
//         var contentXml = File.ReadAllText(contentXmlPath);

//         var rs = new Regex("<ETSapp Name=\"(.*)\" XmlFile=\"(.*)\" \\/>");
//         var match = rs.Match(contentXml);
//         var appName = match.Groups[1].Value;
//         var appXmlFileName = Path.Combine(Path.GetDirectoryName(contentXmlPath), match.Groups[2].Value);

//         var appXmlFilePath = Path.Combine(releaseDirectory, "data", appXmlFileName);
//         var releaseContent = new ReleaseContent(appName, appXmlFilePath);
        
//         rs = new Regex("<Product Name=\"(.*)\" Firmware=\"(.*)\" Processor=\"(.*)\" \\/>");
//         foreach (Match rsMatch in rs.Matches(contentXml))
//         {
//             if (rsMatch.Groups[3].Value != "RP2040")
//                 continue;

//             var firmwareName = rsMatch.Groups[1].Value;
//             var filePathUf2 = Path.Combine(releaseDirectory, "data", rsMatch.Groups[2].Value);
//             var firmware = new ReleaseContentFirmware(firmwareName, filePathUf2);
//             releaseContent.Firmwares.Add(firmware);
//         }

//         return releaseContent;
//     }
}