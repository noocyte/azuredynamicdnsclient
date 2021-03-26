using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Rest.Azure.Authentication;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DynamicDnsClient
{
    public class DnsUpdater
    {
        private readonly SecurityConfig _securityConfig;
        private readonly DnsConfig _dnsConfig;

        public DnsUpdater(SecurityConfig securityConfig, DnsConfig dnsConfig)
        {
            _securityConfig = securityConfig;
            _dnsConfig = dnsConfig;
        }

        public async Task UpdateDns()
        {
            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(_securityConfig.TenantId, _securityConfig.ClientId, _securityConfig.ClientSecret);
            var dnsClient = new DnsManagementClient(serviceCreds)
            {
                SubscriptionId = _dnsConfig.SubscriptionId
            };

            var client = new HttpClient();
            var response = await client.GetAsync("http://icanhazip.com");
            var responseString = await response.Content.ReadAsStringAsync();
            var myIp = responseString.Replace('\n', ' ').Replace(" ", "");
            Console.WriteLine("My IP is: {0}", myIp);

            foreach (var recordSetName in _dnsConfig.RecordSetNames)
            {
                try
                {
                    var recordSet = dnsClient.RecordSets.Get(_dnsConfig.ResourceGroupName, _dnsConfig.ZoneName, recordSetName, RecordType.A);

                    // Add a new record to the local object.  Note that records in a record set must be unique/distinct
                    recordSet.ARecords.Clear();
                    recordSet.ARecords.Add(new ARecord(myIp));

                    // Update the record set in Azure DNS
                    // Note: ETAG check specified, update will be rejected if the record set has changed in the meantime
                    recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(_dnsConfig.ResourceGroupName, _dnsConfig.ZoneName, recordSetName, RecordType.A, recordSet, recordSet.Etag);

                    Console.WriteLine($"success - {recordSetName}");
                }
                catch (System.Exception e)
                {
                    Console.WriteLine($"failed - {recordSetName} - {e}");
                }
            }
        }
    }
}
