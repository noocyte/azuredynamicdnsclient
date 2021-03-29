using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicDnsClient
{
    public class DnsUpdaterJob : CronJobService
    {
        private readonly ILogger<DnsUpdaterJob> _logger;
        private readonly IServiceProvider _serviceProvider;

        public DnsUpdaterJob(IScheduleConfig<DnsUpdaterJob> config, ILogger<DnsUpdaterJob> logger, IServiceProvider serviceProvider) :
            base(config.CronExpression, config.TimeZoneInfo, logger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} {nameof(DnsUpdaterJob)} is working.");
            using var scope = _serviceProvider.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<DnsUpdater>();
            await svc.UpdateDns(cancellationToken);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(DnsUpdaterJob)} starts.");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(DnsUpdaterJob)} stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
