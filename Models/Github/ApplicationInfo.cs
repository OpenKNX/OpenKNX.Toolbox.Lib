using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenKNX.Toolbox.Lib.Models.Github
{
    internal class ApplicationInfo
    {
        public string Repository { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HelpThread { get; set; } = string.Empty;
        public List<ApplicationId> Apps { get; set; } = new List<ApplicationId>();
    }
}
