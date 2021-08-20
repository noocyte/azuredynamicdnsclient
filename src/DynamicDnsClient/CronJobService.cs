// Code credit: https://github.com/dotnet-labs/ServiceWorkerCronJob
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicDnsClient
{
    public abstract class CronJobService : IHostedService, IDisposable
    {
        private System.Timers.Timer _timer;
        private readonly CronExpression _expression;
        private readonly TimeZoneInfo _timeZoneInfo;
        private readonly ILogger _logger;

        protected virtual bool RunOnFirstStart => false;

        protected CronJobService(string cronExpression, TimeZoneInfo timeZoneInfo, ILogger logger)
        {
            _expression = CronExpression.Parse(cronExpression);
            _timeZoneInfo = timeZoneInfo;
            _logger = logger;
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{className} starts.", GetType().Name);
            await ScheduleJob(cancellationToken);
        }

        protected virtual async Task ScheduleJob(CancellationToken cancellationToken)
        {
            if (RunOnFirstStart)
            {
                _logger.LogInformation("Job set to run at first start - will execute before setting up timers");
                await DoWork(cancellationToken);
            }

            var next = _expression.GetNextOccurrence(DateTimeOffset.Now, _timeZoneInfo);
            if (next.HasValue)
            {
                _logger.LogInformation("Next occurance: {occurance}", next.Value);
                var delay = next.Value - DateTimeOffset.Now;
                if (delay.TotalMilliseconds <= 0)   // prevent non-positive values from being passed into Timer
                {
                    await ScheduleJob(cancellationToken);
                }

                _logger.LogInformation("Delay set to: {delay}", delay);

                _timer = new System.Timers.Timer(delay.TotalMilliseconds);
                _timer.Elapsed += async (sender, args) =>
                {
                    _timer.Dispose();  // reset and dispose timer
                    _timer = null;

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("{currentDateTime} {className} is working.", DateTime.Now.ToString("s") , GetType().Name);
                        try
                        {
                            await DoWork(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "{className} failed", GetType().Name);
                        }
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await ScheduleJob(cancellationToken);    // reschedule next
                    }
                };
                _timer.Start();
            }
            await Task.CompletedTask;
        }

        public abstract Task DoWork(CancellationToken cancellationToken);

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{className} stopping.", GetType().Name);
            _timer?.Stop();
            await Task.CompletedTask;
        }

        public virtual void Dispose()
        {
            _timer?.Dispose();
        }
    }

    public interface IScheduleConfig<T>
    {
        string CronExpression { get; set; }
        TimeZoneInfo TimeZoneInfo { get; set; }
    }

    public class ScheduleConfig<T> : IScheduleConfig<T>
    {
        public string CronExpression { get; set; }
        public TimeZoneInfo TimeZoneInfo { get; set; }
    }

    public static class ScheduledServiceExtensions
    {
        public static IServiceCollection AddCronJob<T>(this IServiceCollection services, Action<IScheduleConfig<T>> options) where T : CronJobService
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), @"Please provide Schedule Configurations.");
            }
            var config = new ScheduleConfig<T>();
            options.Invoke(config);
            if (string.IsNullOrWhiteSpace(config.CronExpression))
            {
                throw new ArgumentNullException(nameof(ScheduleConfig<T>.CronExpression), @"Empty Cron Expression is not allowed.");
            }

            services.AddSingleton<IScheduleConfig<T>>(config);
            services.AddHostedService<T>();
            return services;
        }
    }
}
