using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenKNX.Toolbox.Lib.Models
{
    public class RepositoryMapping
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string AppId { get; set; } = string.Empty;


        public RepositoryMapping(string appId, string name, string label)
        {
            Name = name;
            Label = label;
            AppId = appId;
        }
    }
}
