using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lever.Common.Models
{
    public class Sql
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int Tier { get; set; }
        public List<Environment> EnvironmentOverrides { get; set; }
    }
}
