using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DynamicDnsClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.local.json", optional: true)
                .AddEnvironmentVariables();

            var config = builder.Build();

            var dnsConfig = config.GetSection("Dns").Get<DnsConfig>();
            var securityConfig = config.GetSection("Security").Get<SecurityConfig>();
            var updater = new DnsUpdater(securityConfig, dnsConfig);
            await updater.UpdateDns();
        }
    }
}
