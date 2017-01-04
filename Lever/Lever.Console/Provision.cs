using Lever.Common;
using Lever.Common.Helpers;
using Lever.Common.Models;
using Microsoft.Rest;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.Azure.Management.WebSites.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;

namespace Lever.Console
{
    public static class Provision
    {
        public static async Task<ValidationResults> Execute(Validation val, string authKey, string appId)
        {
            var validationResults = new ValidationResults();

            

            Dictionary<string, Microsoft.Azure.Management.WebSites.Models.SiteCollection> GetSitesCache = new Dictionary<string, Microsoft.Azure.Management.WebSites.Models.SiteCollection>();

            Dictionary<string, IEnumerable<Microsoft.Azure.Management.Storage.Models.StorageAccount>> StorageCache = new Dictionary<string, System.Collections.Generic.IEnumerable<Microsoft.Azure.Management.Storage.Models.StorageAccount>>();
            Dictionary<string, Microsoft.Azure.Management.Sql.SqlManagementClient> SqlClientCache = new Dictionary<string, Microsoft.Azure.Management.Sql.SqlManagementClient>();
            Dictionary<string, Microsoft.Azure.Management.Sql.Models.DatabaseListResponse> SqlServerCache = new Dictionary<string, Microsoft.Azure.Management.Sql.Models.DatabaseListResponse>();
            Dictionary<string, Microsoft.Azure.Management.Redis.RedisManagementClient> RedisClientCache = new Dictionary<string, Microsoft.Azure.Management.Redis.RedisManagementClient>();
            Dictionary<string, Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient> KeyVaultClientCache = new Dictionary<string, Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient>();


            ClientCredential credential = new ClientCredential(appId, authKey);
            AuthenticationContext authContext = new AuthenticationContext(String.Format("https://login.windows.net/{0}/", val.TenantId));

            AuthenticationResult tokenResult = await authContext.AcquireTokenAsync("https://management.azure.com/", credential);

            val.Credential = new TokenCredentials(tokenResult.AccessToken);

            Microsoft.Azure.Management.WebSites.WebSiteManagementClient client = new Microsoft.Azure.Management.WebSites.WebSiteManagementClient((TokenCredentials)val.Credential);
            Microsoft.Azure.Management.Storage.StorageManagementClient storClient = new Microsoft.Azure.Management.Storage.StorageManagementClient((TokenCredentials)val.Credential);

            Microsoft.Azure.Management.Sql.SqlManagementClient sqlClient = new Microsoft.Azure.Management.Sql.SqlManagementClient(new TokenCloudCredentials(val.Config.Subscriptions[0].Id, tokenResult.AccessToken));


            foreach (var app in val.Applications)
            {
                foreach (var env in val.Config.Environments)
                {
                    var appEnv = env;
                    {
                        if (app.KeyVault != null)
                        {
                            appEnv = NameHelper.GetAppEnvironment(env, app.KeyVault.EnvironmentOverrides);
                            if (appEnv.Skip.GetValueOrDefault(false) == false)
                            {
                                if (!KeyVaultClientCache.ContainsKey(appEnv.Subscription))
                                {
                                    KeyVaultClientCache.Add(appEnv.Subscription, new Microsoft.Azure.Management.KeyVault.KeyVaultManagementClient((TokenCredentials)val.Credential) { SubscriptionId = val.Config.Subscriptions.Where(c => c.Name == appEnv.Subscription).First().Id });
                                }
                                var vaultName = string.Format("{0}{1}{2}", val.AssetPrefix, app.Redis.Name ?? app.Name, appEnv.Name);
                                var thisKeyVault = await KeyVaultClientCache[appEnv.Subscription].Vaults.GetWithHttpMessagesAsync(appEnv.KeyVaultResourceGroup, vaultName);
                            }
                        }
                        if (app.Redis != null)
                        {
                            if (app.Redis.RedisConnectionStrings == null)
                                app.Redis.RedisConnectionStrings = new Dictionary<string, string>();
                            appEnv = NameHelper.GetAppEnvironment(env, app.Redis.EnvironmentOverrides);

                            if (appEnv.Skip.GetValueOrDefault(false) == false)
                            {
                                if (!RedisClientCache.ContainsKey(appEnv.Subscription))
                                {
                                    RedisClientCache.Add(appEnv.Subscription, new Microsoft.Azure.Management.Redis.RedisManagementClient((TokenCredentials)val.Credential) { SubscriptionId = val.Config.Subscriptions.Where(c => c.Name == appEnv.Subscription).First().Id });
                                }
                                var redisResourceGroup = appEnv.RedisResourceGroup;
                                if (!await CheckForResourceGroup(val, appEnv, redisResourceGroup, validationResults))
                                {
                                    validationResults.Issues.Add(new Issue { IssueType = "Redis", Name = redisResourceGroup, AppName = app.Name, Description = $"Resource Group does not exist" });
                                }
                                else
                                {
                                    var cacheName = string.Format("{0}{1}{2}", val.AssetPrefix, app.Redis.Name ?? app.Name, appEnv.Name);
                                    Microsoft.Azure.Management.Redis.Models.RedisResource thisCache = null;
                                    try
                                    {
                                        thisCache = (await RedisClientCache[appEnv.Subscription].Redis.GetWithHttpMessagesAsync(redisResourceGroup, cacheName)).Body;
                                    }
                                    catch
                                    {
                                    }
                                    if (thisCache == null)
                                    {
                                        validationResults.Issues.Add(new Issue { IssueType = "Redis", Name = cacheName, AppName = app.Name, Description = $"Redis not found in RG {redisResourceGroup}" });
                                        if (val.Mode == Constants.PROVISION_MODE)
                                        {
                                            var createParams = new Microsoft.Azure.Management.Redis.Models.RedisCreateParameters { Location = appEnv.Region, Sku = new Microsoft.Azure.Management.Redis.Models.Sku { Name = "Basic" } };
                                            createParams.Sku.Family = appEnv.RedisTier.Substring(0, 1);
                                            createParams.Sku.Capacity = int.Parse(appEnv.RedisTier.Substring(1, 1));
                                            if (appEnv.RedisIsReplicated.GetValueOrDefault(false))
                                            {
                                                createParams.Sku.Name = "Standard";
                                            }
                                            thisCache = (await RedisClientCache[appEnv.Subscription].Redis.CreateWithHttpMessagesAsync(redisResourceGroup, cacheName, createParams)).Body;
                                            validationResults.Actions.Add(new Common.Models.Action { AppName = appEnv.Name, Name = "Redis", Description = $"Created Redis {cacheName} in Region {appEnv.Region} and RG {redisResourceGroup}" });
                                        }
                                    }
                                    if (thisCache != null)
                                    {
                                        if (appEnv.RedisIsReplicated.HasValue)
                                        {
                                            if ((appEnv.RedisIsReplicated.Value && thisCache.Sku.Name == "Basic") || (appEnv.RedisIsReplicated.Value == false && thisCache.Sku.Name != "Basic"))
                                            {
                                                validationResults.Issues.Add(new Issue { IssueType = "Redis", Name = cacheName, AppName = app.Name, Description = $"Redis replication is {thisCache.Sku.Name != "Basic"} but should be {appEnv.RedisIsReplicated.Value}" });
                                            }
                                        }
                                        if (appEnv.RedisTier != null && appEnv.RedisTier != (thisCache.Sku.Family + thisCache.Sku.Capacity))
                                        {
                                            validationResults.Issues.Add(new Issue { IssueType = "Redis", Name = cacheName, AppName = app.Name, Description = $"Redis tier is {thisCache.Sku.Family + thisCache.Sku.Capacity} but should be {appEnv.RedisTier}" });
                                        }
                                        //Pull its conn str
                                        var keys = await RedisClientCache[appEnv.Subscription].Redis.ListKeysWithHttpMessagesAsync(redisResourceGroup, cacheName);
                                        app.Redis.RedisConnectionStrings.Add(appEnv.Name, string.Format("{0}.redis.cache.windows.net:6380,abortConnect=false,ssl=true,password={1}", cacheName, keys.Body.PrimaryKey));
                                    }

                                }
                            }
                        }
                        if (app.Sql != null)
                        {
                            appEnv = NameHelper.GetAppEnvironment(env, app.Sql.EnvironmentOverrides);
                            if (appEnv.Skip.GetValueOrDefault(false) == false)
                            {
                                var sqlDbResourceGroup = string.Format("{0}{1}", appEnv.SqlResourceGroup, app.Sql.Tier);

                                if (!SqlClientCache.ContainsKey(appEnv.Subscription))
                                {
                                    SqlClientCache.Add(appEnv.Subscription, new Microsoft.Azure.Management.Sql.SqlManagementClient(new TokenCloudCredentials(val.Config.Subscriptions.Where(c => c.Name == appEnv.Subscription).First().Id, tokenResult.AccessToken)));
                                }
                                if (!await CheckForResourceGroup(val, appEnv, sqlDbResourceGroup, validationResults))
                                {
                                    validationResults.Issues.Add(new Issue { IssueType = "Sql", Name = sqlDbResourceGroup, AppName = app.Name, Description = $"Resource Group does not exist" });
                                }
                                else
                                {
                                    var serverName = string.Format("{0}{1}", val.AssetPrefix, appEnv.Subscription.ToLower());
                                    if (!SqlServerCache.ContainsKey(serverName))
                                    {
                                        SqlServerCache.Add(serverName, await SqlClientCache[appEnv.Subscription].Databases.ListAsync(sqlDbResourceGroup, serverName, new CancellationToken()));
                                    }
                                    var dbName = NameHelper.CalcDatabaseName(appEnv, app);
                                    var thisDatabase = SqlServerCache[serverName].FirstOrDefault(c => string.Compare(c.Name, dbName, true) == 0);
                                    if (thisDatabase != null)
                                    {
                                        if (app.Sql.Type != null && thisDatabase.Properties.ServiceObjective != app.Sql.Type)
                                        {
                                            validationResults.Issues.Add(new Issue { IssueType = "Sql", Name = dbName, AppName = app.Name, Description = $"Type is {thisDatabase.Properties.ServiceObjective} instead of {app.Sql.Type}" });
                                        }
                                    }
                                    else
                                    {
                                        validationResults.Issues.Add(new Issue { IssueType = "Sql", Name = dbName, AppName = app.Name, Description = $"Database not found in Server {serverName}" });
                                    }
                                }
                            }
                        }
                        if (app.Storage != null)
                        {
                            appEnv = NameHelper.GetAppEnvironment(env, app.Storage.EnvironmentOverrides);
                            storClient.SubscriptionId = val.Config.Subscriptions.Where(c => c.Name == appEnv.Subscription).First().Id;
                            if (appEnv.Skip.GetValueOrDefault(false) == false)
                            {
                                if (!await CheckForResourceGroup(val, appEnv, appEnv.StorageResourceGroup, validationResults))
                                {
                                    validationResults.Issues.Add(new Issue { IssueType = "Storage", Name = appEnv.StorageResourceGroup, AppName = app.Name, Description = $"Resource Group does not exist" });
                                }
                                else
                                {
                                    if (!StorageCache.ContainsKey(storClient.SubscriptionId + appEnv.StorageResourceGroup))
                                    {
                                        StorageCache.Add(storClient.SubscriptionId + appEnv.StorageResourceGroup, (await storClient.StorageAccounts.ListByResourceGroupWithHttpMessagesAsync(appEnv.StorageResourceGroup)).Body);
                                    }
                                    var calcStorageName = NameHelper.CalcStorageName(val, env, app);
                                    var thisStorage = StorageCache[storClient.SubscriptionId + appEnv.StorageResourceGroup].FirstOrDefault(c => c.Name == calcStorageName);
                                    if (thisStorage != null)
                                    {

                                    }
                                    else
                                    {
                                        validationResults.Issues.Add(new Issue { IssueType = "Storage", Name = calcStorageName, AppName = app.Name, Description = $"Storage not found in RG {appEnv.StorageResourceGroup}" });
                                    }
                                }
                            }
                        }

                        if (app.Site != null)
                        {
                            appEnv = NameHelper.GetAppEnvironment(env, app.Site.EnvironmentOverrides);
                            client.SubscriptionId = val.Config.Subscriptions.Where(c => c.Name == appEnv.Subscription).First().Id;
                            if (appEnv.Skip.GetValueOrDefault(false) == false)
                            {
                                var rg = NameHelper.CalcWebResourceGroup(appEnv, app);
                                if (!await CheckForResourceGroup(val, appEnv, rg, validationResults))
                                {
                                    validationResults.Issues.Add(new Issue { IssueType = "Web", Name = rg, AppName = app.Name, Description = $"Resource Group does not exist" });
                                }
                                else
                                {
                                    if (!GetSitesCache.ContainsKey(client.SubscriptionId + rg))
                                    {
                                        try
                                        {
                                            GetSitesCache.Add(client.SubscriptionId + rg, (await client.Sites.GetSitesWithHttpMessagesAsync(rg)).Body);
                                        }
                                        catch
                                        {
                                            string.Format("GetSitesCache.Add({0} + {1}, (await client.Sites.GetSitesWithHttpMessagesAsync({1})).Body); //For App: {2} Env: {3}", client.SubscriptionId, rg, app.Name, appEnv.Name);
                                        }
                                    }

                                    var calcSiteName = NameHelper.CalcWebsiteName(val, appEnv, app);

                                    //Check for App Plan
                                    var appEnvWebFarm = appEnv.WebServerFarms.FirstOrDefault(c => c.Id == appEnv.DefaultWebFarmId);
                                    var appPlanName = string.Format("{0}{1}", val.AssetPrefix, appEnv.Name, appEnvWebFarm.Id);
                                    Microsoft.Azure.Management.WebSites.Models.ServerFarmWithRichSku appPlan = null;
                                    try
                                    {
                                        appPlan = (await client.ServerFarms.GetServerFarmWithHttpMessagesAsync(rg, appPlanName)).Body;
                                    }
                                    catch { }
                                    if (appPlan == null)
                                    {
                                        validationResults.Issues.Add(new Issue { IssueType = "AppPlan", Name = appPlanName, AppName = app.Name, Description = $"Not Found in RG {rg}" });
                                        if (val.Mode == Constants.PROVISION_MODE)
                                        {
                                            var appPlanSku = new Microsoft.Azure.Management.WebSites.Models.ServerFarmWithRichSku
                                            {
                                                Name = appPlanName,
                                                Location = appEnv.Region,
                                                Sku = new Microsoft.Azure.Management.WebSites.Models.SkuDescription { Capacity = appEnvWebFarm.Sku.Capacity, Name = appEnvWebFarm.Sku.Name, Family = appEnvWebFarm.Sku.Family, Size = appEnvWebFarm.Sku.Size, Tier = appEnvWebFarm.Sku.Tier }
                                            };
                                            var createResult = (await client.ServerFarms.CreateOrUpdateServerFarmWithHttpMessagesAsync(rg, appPlanName, appPlanSku));
                                            appPlan = createResult.Body;
                                            validationResults.Actions.Add(new Common.Models.Action { AppName = appEnv.Name, Name = "AppPlan", Description = $"Created AppPlan {appPlanName} in Region {appEnv.Region} and RG {rg}" });
                                        }
                                    }
                                    var thisSite = GetSitesCache[client.SubscriptionId + rg].Value.Where(c => string.Compare(c.Name, calcSiteName, true) == 0).FirstOrDefault();
                                    if (thisSite == null)
                                    {
                                        validationResults.Issues.Add(new Issue { IssueType = "Web", Name = calcSiteName, AppName = app.Name, Description = $"Not Found in RG {rg}" });
                                        if (val.Mode == Constants.PROVISION_MODE)
                                        {
                                            var siteEnvelope = new Microsoft.Azure.Management.WebSites.Models.Site { Location = appEnv.Region, ServerFarmId = appPlan.Id, Name = calcSiteName };
                                            var createResult = (await client.Sites.CreateOrUpdateSiteWithHttpMessagesAsync(rg, calcSiteName, siteEnvelope));
                                            thisSite = createResult.Body;
                                            validationResults.Actions.Add(new Common.Models.Action { AppName = appEnv.Name, Name = "Web", Description = $"Created Web {calcSiteName} in Region {appEnv.Region} and RG {rg}" });
                                        }
                                    }
                                    if (thisSite != null)
                                    {
                                        if (app.Redis != null && app.Redis.RedisConnectionStrings.ContainsKey(appEnv.Name))
                                        {
                                            var siteConfig = (await client.Sites.ListSiteConnectionStringsWithHttpMessagesAsync(rg, calcSiteName)).Body;
                                            if (siteConfig.Properties == null)
                                            {
                                                siteConfig.Properties = new Dictionary<string, Microsoft.Azure.Management.WebSites.Models.ConnStringValueTypePair>();
                                            }
                                            var redisConnStringName = "REDISCONNSTR_1";
                                            if (!siteConfig.Properties.ContainsKey(redisConnStringName))
                                            {
                                                validationResults.Issues.Add(new Issue { IssueType = "Web", Name = calcSiteName, AppName = app.Name, Description = $"Redis Conn String {redisConnStringName} not set" });
                                                siteConfig.Properties.Add(redisConnStringName, new ConnStringValueTypePair(DatabaseServerType.Custom, app.Redis.RedisConnectionStrings[appEnv.Name]));
                                                //Set its connection string
                                                var updateResult = await client.Sites.UpdateSiteConnectionStringsWithHttpMessagesAsync(rg, calcSiteName, siteConfig);
                                                validationResults.Actions.Add(new Common.Models.Action { AppName = appEnv.Name, Name = "Web", Description = $"Set Redis Conn String to {redisConnStringName}" });
                                            }
                                        }
                                        if (app.Site.ClientCertEnabled.HasValue && (app.Site.ClientCertEnabled != thisSite.ClientCertEnabled.GetValueOrDefault(false)))
                                        {
                                            validationResults.Issues.Add(new Issue { IssueType = "Web", Name = calcSiteName, AppName = app.Name, Description = string.Format("ClientCertEnabled is not set to {0}", app.Site.ClientCertEnabled) });
                                            //So Set it
                                            thisSite.ClientCertEnabled = app.Site.ClientCertEnabled;
                                            //await client.Sites.CreateOrUpdateSiteWithHttpMessagesAsync(thisSite.ResourceGroup, thisSite.Name, thisSite);
                                        }
                                        if (app.Site.ClientAffinityEnabled.HasValue && app.Site.ClientAffinityEnabled != thisSite.ClientAffinityEnabled)
                                        {
                                            validationResults.Issues.Add(new Issue { IssueType = "Web", Name = calcSiteName, AppName = app.Name, Description = $"ClientAffinityEnabled should be {app.Site.ClientAffinityEnabled} but is {thisSite.ClientAffinityEnabled}" });
                                        }
                                        if (app.Site.Ssl.HasValue)
                                        {
                                            //Check for Cert
                                            if (!thisSite.HostNameSslStates.Any(c => c.Name.Contains(appEnv.DomainName)))
                                            {
                                                validationResults.Issues.Add(new Issue { IssueType = "Web", Name = calcSiteName, AppName = app.Name, Description = $"Site is missing SSL Cert" });
                                            }
                                            //Check for State
                                            if (!thisSite.HostNameSslStates.Any(c => c.SslState.GetValueOrDefault() == Microsoft.Azure.Management.WebSites.Models.SslState.SniEnabled))
                                            {
                                                validationResults.Issues.Add(new Issue { IssueType = "Web", Name = calcSiteName, AppName = app.Name, Description = $"Site SSL is not enabled" });
                                            }
                                        }

                                    }

                                }
                            }
                        }
                    }
                }
                if (!validationResults.Issues.Any(c => c.AppName == app.Name))
                {
                    validationResults.ValidApplications.Add(app.Name);
                }
            }
            return validationResults;
        }

        public static async Task<bool> CheckForResourceGroup(Validation val, Common.Models.Environment appEnv, string rg, ValidationResults res)
        {
            var client = new Microsoft.Azure.Management.ResourceManager.ResourceManagementClient((TokenCredentials)val.Credential);
            client.SubscriptionId = val.Config.Subscriptions.Where(c => c.Name == appEnv.Subscription).First().Id;
            var result = (await client.ResourceGroups.CheckExistenceWithHttpMessagesAsync(rg)).Body;
            if (!result && val.Mode == Constants.PROVISION_MODE)
            {
                var createResult = await client.ResourceGroups.CreateOrUpdateWithHttpMessagesAsync(rg, new Microsoft.Azure.Management.ResourceManager.Models.ResourceGroup { Location = appEnv.Region, Name = rg });
                res.Actions.Add(new Common.Models.Action { AppName = appEnv.Name, Name = "ResourceGroup", Description = $"Created RG {rg} in Region {appEnv.Region}" });
                return true;
            }
            return (await client.ResourceGroups.CheckExistenceWithHttpMessagesAsync(rg)).Body;
        }
    }
}
