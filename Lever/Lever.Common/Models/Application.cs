using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lever.Common.Models
{
    public class Application
    {
        public string Name { get; set; }
        public WebSite Site { get; set; }
        public Storage Storage { get; set; }
        public Sql Sql { get; set; }
        public Redis Redis { get; set; }
        public ServiceBus ServiceBus { get; set; }
        public KeyVault KeyVault { get; set; }

    }
}
