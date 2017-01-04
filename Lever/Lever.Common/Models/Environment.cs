using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lever.Common.Models
{
    public class Environment
    {
        public Environment()
        {

        }
        public Environment(Environment env)
        {
            Name = env.Name;
            ApiResourceGroup = env.ApiResourceGroup;
            FunctionResourceGroup = env.FunctionResourceGroup;
            Subscription = env.Subscription;
            StorageResourceGroup = env.StorageResourceGroup;
            Region = env.Region;
            SqlResourceGroup = env.SqlResourceGroup;
            RedisResourceGroup = env.RedisResourceGroup;
            RedisIsReplicated = env.RedisIsReplicated;
            RedisTier = env.RedisTier;
            KeyVaultResourceGroup = env.KeyVaultResourceGroup;
            DomainName = env.DomainName;
            NameAbbr = env.NameAbbr;
            DefaultWebFarmId = env.DefaultWebFarmId;
            WebServerFarms = env.WebServerFarms;
        }
        public void Override(Environment env)
        {
            ApiResourceGroup = env.ApiResourceGroup ?? ApiResourceGroup;
            FunctionResourceGroup = env.FunctionResourceGroup ?? FunctionResourceGroup;
            Region = env.Region ?? Region;
            Subscription = env.Subscription ?? Subscription;
            StorageResourceGroup = env.StorageResourceGroup ?? StorageResourceGroup;
            Skip = env.Skip ?? Skip;
            SqlResourceGroup = env.SqlResourceGroup ?? SqlResourceGroup;
            RedisResourceGroup = env.RedisResourceGroup ?? RedisResourceGroup;
            RedisIsReplicated = env.RedisIsReplicated ?? RedisIsReplicated;
            RedisTier = env.RedisTier ?? RedisTier;
            KeyVaultResourceGroup = env.KeyVaultResourceGroup ?? KeyVaultResourceGroup;
            DomainName = env.DomainName ?? DomainName;
            NameAbbr = env.NameAbbr ?? NameAbbr;
            DefaultWebFarmId = env.DefaultWebFarmId ?? DefaultWebFarmId;
            WebServerFarms = env.WebServerFarms ?? WebServerFarms;
        }
        public string Name { get; set; }
        public string ApiResourceGroup { get; set; }
        public string FunctionResourceGroup { get; set; }
        public string Subscription { get; set; }
        public string Region { get; set; }
        public string StorageResourceGroup { get; set; }
        public bool? Skip { get; set; }
        public string SqlResourceGroup { get; set; }
        public string RedisResourceGroup { get; set; }
        public bool? RedisIsReplicated { get; set; }
        public string RedisTier { get; set; }
        public string KeyVaultResourceGroup { get; set; }
        public string DomainName { get; set; }
        public string NameAbbr { get; set; }
        public string DefaultWebFarmId { get; set; }
        public List<WebServerFarm> WebServerFarms { get; set; }

    }
}
