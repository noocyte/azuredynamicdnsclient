using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace DynamicDnsClient
{
    class Program
    {
        private const string _defaultCronExpression = "0 */2 * * *"; // every other hour

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
                       var configCronExpression = del.Configuration.GetSection("CronExpression").Value;
                       var cron = string.IsNullOrWhiteSpace(configCronExpression)
                               ? _defaultCronExpression
                               : configCronExpression;

                       opt.CronExpression = cron;
                       opt.TimeZoneInfo = TimeZoneInfo.Utc;
                   });
                   collection.AddTransient<DnsUpdater>();
               })
               .RunConsoleAsync();
        }
    }
}
