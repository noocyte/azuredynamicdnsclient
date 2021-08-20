using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicDnsClient.Dns
{
    public class DnsUpdaterJob : CronJobService
    {
        private readonly IServiceProvider _serviceProvider;

        public DnsUpdaterJob(
            IScheduleConfig<DnsUpdaterJob> config,
            ILogger<DnsUpdaterJob> logger,
            IServiceProvider serviceProvider) : base(config.CronExpression, config.TimeZoneInfo, logger)
        {
            _serviceProvider = serviceProvider;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            var svc = _serviceProvider.GetRequiredService<DnsUpdater>();
            await svc.UpdateDns(cancellationToken);
        }
    }
}
