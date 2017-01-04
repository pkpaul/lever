using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lever.Common.Models
{
    public class Config
    {
        public List<Environment> Environments { get; set; }
        public List<Subscription> Subscriptions { get; set; }
    }
}
