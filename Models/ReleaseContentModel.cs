using OpenKNX.Toolbox.Lib.Data;
using System.Collections.ObjectModel;

namespace OpenKNX.Toolbox.Lib.Models;

public class ReleaseContentModel
{
    public ObservableCollection<Product> Products { get; set; } = new ();
    public string XmlFile { get; set; } = "";
}