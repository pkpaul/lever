using Lever.Common.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lever.Console
{
    class Program
    {
        static void Main(string[] args)
        {
           
            Validation val = Newtonsoft.Json.JsonConvert.DeserializeObject<Validation>(File.ReadAllText(args[0]));

            Provision.Execute(val, ConfigurationManager.AppSettings.Get("AppKey"), ConfigurationManager.AppSettings.Get("AppId")).Wait();
        }
    }
}
