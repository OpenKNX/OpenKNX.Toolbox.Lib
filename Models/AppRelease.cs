using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace OpenKNX.Toolbox.Lib.Models
{
    public class AppRelease
    {
        public string Name { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public bool IsPrerelease { get; set; } = false;
        public SemanticVersion Version { get; set; }

        public AppRelease(string name, string url, SemanticVersion version)
        {
            Version = version;
            Name = name;
            FileUrl = url;
        }
    }
}
