using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lever.Common.Models
{
    public class WebSite
    {

        public string WebSiteType { get; set; }
        public bool? ClientCertEnabled { get; set; }
        public bool? Ssl { get; set; }
        public bool? ClientAffinityEnabled { get; set; }
        public List<Environment> EnvironmentOverrides { get; set; }
    }
}
