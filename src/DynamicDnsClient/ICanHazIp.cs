using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicDnsClient
{
    public class ICanHazIp: IGetCurrentIpAddress
    {
        private readonly ILogger<ICanHazIp> _logger;
        public ICanHazIp(ILogger<ICanHazIp> logger)
        {
            _logger = logger;
        }
        public async Task<string> GetCurrentIdAsync(CancellationToken cancellationToken)
        {
            var client = new HttpClient();
            var response = await client.GetAsync("http://icanhazip.com", cancellationToken);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            var currentIp = responseString.Replace('\n', ' ').Replace(" ", "");
            _logger.LogInformation("My IP is: {currentIp}", currentIp);
            return currentIp;
        }
    }
}
