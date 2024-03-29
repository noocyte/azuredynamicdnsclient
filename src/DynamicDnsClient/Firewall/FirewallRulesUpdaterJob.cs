﻿using DynamicDnsClient.Dns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicDnsClient.Firewall
{
    public class FirewallRulesUpdaterJob : CronJobService
    {
        private readonly IServiceProvider _serviceProvider;

        public FirewallRulesUpdaterJob(
            IScheduleConfig<FirewallRulesUpdaterJob> config,
            ILogger<FirewallRulesUpdaterJob> logger,
            IServiceProvider serviceProvider) : base(config.CronExpression, config.TimeZoneInfo, logger)
        {
            _serviceProvider = serviceProvider;
        }

        protected override bool RunOnFirstStart => true;

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            var svc = _serviceProvider.GetRequiredService<FirewallRulesUpdater>();
            await svc.UpdateFirewall(cancellationToken);
        }
    }
}
