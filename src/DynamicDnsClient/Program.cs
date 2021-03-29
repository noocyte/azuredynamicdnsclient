using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DynamicDnsClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
               .ConfigureHostConfiguration(config =>
               {
                   config.SetBasePath(Directory.GetCurrentDirectory())
                       .AddJsonFile("appsettings.json")
                       .AddJsonFile("appsettings.local.json", optional: true)
                       .AddEnvironmentVariables();
               })
               .ConfigureServices((del, collection) =>
               {
                   collection.Configure<DnsConfig>(del.Configuration.GetSection("Dns"));
                   collection.Configure<SecurityConfig>(del.Configuration.GetSection("Security"));
                   collection.AddCronJob<DnsUpdaterJob>(opt =>
                   {
                       opt.CronExpression = "0 * * * *";
                       opt.TimeZoneInfo = TimeZoneInfo.Utc;
                   });
               })
               .RunConsoleAsync();
        }
    }
}
