using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Rest.Azure.Authentication;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicDnsClient
{
    public class DnsUpdater
    {
        private readonly SecurityConfig _securityConfig;
        private readonly DnsConfig _dnsConfig;
        private readonly ILogger<DnsUpdater> _logger;

        public DnsUpdater(IOptions<SecurityConfig> securityConfig, IOptions<DnsConfig> dnsConfig,
            ILogger<DnsUpdater> logger)
        {
            _securityConfig = securityConfig.Value;
            _dnsConfig = dnsConfig.Value;
            _logger = logger;
        }

        public async Task UpdateDns(CancellationToken cancellationToken)
        {
            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(_securityConfig.TenantId, _securityConfig.ClientId, _securityConfig.ClientSecret);
            var dnsClient = new DnsManagementClient(serviceCreds)
            {
                SubscriptionId = _dnsConfig.SubscriptionId
            };

            var client = new HttpClient();
            var response = await client.GetAsync("http://icanhazip.com", cancellationToken);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            var currentIp = responseString.Replace('\n', ' ').Replace(" ", "");
            _logger.LogInformation("My IP is: {0}", currentIp);

            foreach (var recordSetName in _dnsConfig.RecordSetNames)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Cancellation requested, aborting update");
                    return;
                }

                _logger.LogInformation($"Trying to update: {recordSetName}");

                try
                {
                    var recordSet = dnsClient.RecordSets.Get(_dnsConfig.ResourceGroupName, _dnsConfig.ZoneName, recordSetName, RecordType.A);

                    // Add a new record to the local object.  Note that records in a record set must be unique/distinct

                    // first we check if we need to update - no need to do it all the time
                    var currentARecord = recordSet.ARecords.FirstOrDefault();
                    if (currentARecord != null)
                    {
                        if (currentARecord.Ipv4Address.Equals(currentIp))
                        {
                            _logger.LogInformation("Current IP already set, trying next recordset.");
                            continue;
                        }
                    }

                    recordSet.ARecords.Clear();
                    recordSet.ARecords.Add(new ARecord(currentIp));

                    // Update the record set in Azure DNS
                    // Note: ETAG check specified, update will be rejected if the record set has changed in the meantime
                    recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(_dnsConfig.ResourceGroupName, _dnsConfig.ZoneName, recordSetName, RecordType.A, recordSet, recordSet.Etag, cancellationToken: cancellationToken);

                    _logger.LogInformation($"Success - {recordSetName}");
                }
                catch (System.Exception e)
                {
                    _logger.LogError(e, $"Failed - {recordSetName}");
                }
            }
        }
    }
}
