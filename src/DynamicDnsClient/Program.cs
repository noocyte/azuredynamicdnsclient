using DynamicDnsClient.Cloudflare;
using DynamicDnsClient.Dns;
using DynamicDnsClient.Firewall;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicDnsClient;

class Program
{
    private const string _defaultCronExpression = "0 */2 * * *"; // every other hour

    static async Task Main(string[] args)
    {
        await Host
           .CreateDefaultBuilder(args)
           .ConfigureServices((ctx, collection) =>
           {
               // config
               collection.Configure<FirewallConfig>(ctx.Configuration.GetSection("Firewall"));
               collection.Configure<DnsConfig>(ctx.Configuration.GetSection("Dns"));
               collection.Configure<SecurityConfig>(ctx.Configuration.GetSection("Security"));
               collection.Configure<CloudflareConfig>(ctx.Configuration.GetSection("Cloudflare"));

               // firewall
               collection.AddCronJob<FirewallRulesUpdaterJob>(opt =>
               {
                   var configCronExpression = ctx.Configuration.GetSection("CronExpression").Value;
                   var cron = string.IsNullOrWhiteSpace(configCronExpression)
                           ? _defaultCronExpression
                           : configCronExpression;

                   opt.CronExpression = cron;
                   opt.TimeZoneInfo = TimeZoneInfo.Utc;
               });
               collection.AddTransient<FirewallRulesUpdater>();

               // cloudflare
               collection.AddCronJob<CloudflareUpdaterJob>(opt =>
               {
                   var configCronExpression = ctx.Configuration.GetSection("CronExpression").Value;
                   var cron = string.IsNullOrWhiteSpace(configCronExpression)
                           ? _defaultCronExpression
                           : configCronExpression;

                   opt.CronExpression = cron;
                   opt.TimeZoneInfo = TimeZoneInfo.Utc;
               });
               collection.AddTransient<CloudflareUpdater>();
               collection.AddHttpClient();

               // services
               collection.AddTransient<IGetCurrentIpAddress, ICanHazIp>();
               
               // uncomment to test random stuff locally... 
               //collection.AddHostedService<Tester>();
           })
           .RunConsoleAsync();
    }
}

internal class Tester(IServiceScopeFactory factory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scope = factory.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<CloudflareUpdater>();
        await updater.UpdateRules(stoppingToken);
    }
}
