using OpenKNX.Toolbox.Lib.Data;
using System.Collections.ObjectModel;

namespace OpenKNX.Toolbox.Lib.Models;

public class ReleaseContentModel
{
    public ObservableCollection<Product> Products { get; set; } = new ();
    public string XmlFile { get; set; } = "";
    public string RepositoryName { get; set; } = "";
    public string ReleaseName { get; set; } = "";
    public DateTimeOffset? Published { get; set; }
    public bool IsPrerelease { get; set; } = false;
}