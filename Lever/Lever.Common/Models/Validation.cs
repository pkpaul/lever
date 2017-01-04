using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Lever.Common.Models
{
    public class Validation
    {
        public Config Config { get; set; }
        public string Mode { get; set; }
        public string AssetPrefix { get; set; }
        public string TenantId { get; set; }
        [JsonIgnore]
        public object Credential { get; set; }
        public List<Application> Applications { get; set; }
    }
}
