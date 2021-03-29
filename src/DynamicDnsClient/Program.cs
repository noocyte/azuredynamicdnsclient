using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace DynamicDnsClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Host
               .CreateDefaultBuilder(args)
               .ConfigureServices((del, collection) =>
               {
                   collection.Configure<DnsConfig>(del.Configuration.GetSection("Dns"));
                   collection.Configure<SecurityConfig>(del.Configuration.GetSection("Security"));
                   collection.AddCronJob<DnsUpdaterJob>(opt =>
                   {
                       opt.CronExpression = "0 * * * *";
                       opt.TimeZoneInfo = TimeZoneInfo.Utc;
                   });
                   collection.AddTransient<DnsUpdater>();
               })
               .RunConsoleAsync();
        }
    }
}
