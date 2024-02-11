using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Azure.Management.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Rest.Azure.Authentication;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicDnsClient.Dns;

public class DnsUpdater(
    IOptions<SecurityConfig> securityConfig,
    IOptions<DnsConfig> dnsConfig,
    IGetCurrentIpAddress ipAddressLookup,
    ILogger<DnsUpdater> logger)
{
    private readonly SecurityConfig _securityConfig = securityConfig.Value;
    private readonly DnsConfig _dnsConfig = dnsConfig.Value;

    public async Task UpdateDns(CancellationToken cancellationToken)
    {

        var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(_securityConfig.TenantId, _securityConfig.ClientId, _securityConfig.ClientSecret);
        var dnsClient = new DnsManagementClient(serviceCreds)
        {
            SubscriptionId = _securityConfig.SubscriptionId
        };

        var currentIp = await ipAddressLookup.GetCurrentIdAsync(cancellationToken);

        foreach (var recordSetName in _dnsConfig.RecordSetNames)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Cancellation requested, aborting update");
                return;
            }

            logger.LogInformation($"Trying to update: {recordSetName}");

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
                        logger.LogInformation("Current IP already set, trying next recordset.");
                        continue;
                    }
                }

                recordSet.ARecords.Clear();
                recordSet.ARecords.Add(new ARecord(currentIp));

                // Update the record set in Azure DNS
                // Note: ETAG check specified, update will be rejected if the record set has changed in the meantime
                recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(_dnsConfig.ResourceGroupName, _dnsConfig.ZoneName, recordSetName, RecordType.A, recordSet, recordSet.Etag, cancellationToken: cancellationToken);

                logger.LogInformation($"Success - {recordSetName}");
            }
            catch (System.Exception e)
            {
                logger.LogError(e, $"Failed - {recordSetName}");
            }
        }
    }
}
