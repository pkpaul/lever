using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lever.Common.Models
{
    public class Redis
    {
        public Redis()
        {
            RedisConnectionStrings = new Dictionary<string, string>();
        }
        public string Name { get; set; }
        public List<Environment> EnvironmentOverrides { get; set; }
        [JsonIgnore]
        public Dictionary<string, string> RedisConnectionStrings { get; set; }
    }
}
