using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lever.Common.Models
{
    public class WebServerFarmSku
    {
        public string Name { get; set; }
        public string Tier { get; set; }
        public string Size { get; set; }
        public string Family { get; set; }
        public int Capacity { get; set; }
    }
}
