using Lever.Common.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lever.Common.Helpers
{
    public static class NameHelper
    {
        public static Environment GetAppEnvironment(Environment env, List<Environment> overrides = null)
        {
            Environment result = new Environment(env);
            if (overrides != null && overrides.Any(c => c.Name == env.Name))
            {
                result.Override(overrides.First(c => c.Name == env.Name));
            }
            return result;
        }

        public static string CalcDatabaseName(Environment env, Application app)
        {
            if (env.Name == "quarterly" || env.Name == "monthly")
                return string.Format("{0}{1}QA", app.Name, env.Name);
            return string.Format("{0}{1}", app.Name, env.Name);
        }

        public static string CalcStorageName(Validation val, Environment env, Application app)
        {
            if (env.Region == "East US")
                return string.Format("{0}{1}{2}", val.AssetPrefix, app.Storage.Name ?? app.Name, env.NameAbbr);
            return string.Format("{0}{1}{2}{3}", val.AssetPrefix, app.Storage.Name ?? app.Name, env.NameAbbr, "nc");
        }

        public static string CalcWebResourceGroup(Environment env, Application app)
        {
            switch (app.Site.WebSiteType)
            {
                case "functionapp": return env.FunctionResourceGroup;
                default: return env.ApiResourceGroup;
            }
        }
        public static string CalcWebsiteName(Validation val, Environment env, Application app)
        {
            switch (env.Region)
            {
                default: return string.Format("{2}{0}nc{1}", app.Name, CalcWebsiteEnvName(env, app), val.AssetPrefix);
            }
        }
        public static string CalcWebsiteEnvName(Environment env, Application app)
        {
            if (app.Site.WebSiteType == "function")
                return env.NameAbbr;
            return env.Name;
        }
    }
}
