using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicDnsClient.Cloudflare;

internal class CloudflareUpdaterJob(
       IScheduleConfig<CloudflareUpdaterJob> config,
       ILogger<CloudflareUpdaterJob> logger,
       IServiceProvider serviceProvider) : CronJobService(config.CronExpression, config.TimeZoneInfo, logger)
{
    public override async Task DoWork(CancellationToken cancellationToken)
    {
        var svc = serviceProvider.GetRequiredService<CloudflareUpdater>();
        await svc.UpdateRules(cancellationToken);
    }
}
