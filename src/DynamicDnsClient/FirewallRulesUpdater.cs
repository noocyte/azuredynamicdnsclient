using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Rest.Azure.Authentication;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicDnsClient
{
    public class FirewallRulesUpdater
    {
        private readonly SecurityConfig _securityConfig;
        private readonly FirewallConfig _firewallConfig;
        private readonly ILogger<FirewallRulesUpdater> _logger;
        private readonly IGetCurrentIpAddress _ipAddressLookup;

        public FirewallRulesUpdater(
            IOptions<SecurityConfig> securityConfig, 
            IOptions<FirewallConfig> firewallConfig, 
            IGetCurrentIpAddress ipAddressLookup,
            ILogger<FirewallRulesUpdater> logger)
        {
            _securityConfig = securityConfig.Value;
            _firewallConfig = firewallConfig.Value;
            _ipAddressLookup = ipAddressLookup;
            _logger = logger;
        }

        public async Task UpdateFirewall(CancellationToken cancellationToken)
        {
            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(_securityConfig.TenantId, _securityConfig.ClientId, _securityConfig.ClientSecret);
            var currentIp = await _ipAddressLookup.GetCurrentIdAsync(cancellationToken);

            var storClient = new StorageManagementClient(serviceCreds)
            {
                SubscriptionId = _securityConfig.SubscriptionId
            };

            var sProp = storClient.StorageAccounts.GetProperties(_firewallConfig.ResourceGroupName, _firewallConfig.AccountName);
            var rules = sProp.NetworkRuleSet;
            var currentIpAddresses = string.Join(',', rules.IpRules.Select(r => r.IPAddressOrRange));
            _logger.LogInformation("Current allowed IP addresses: {currentIpAddesses}", currentIpAddresses);

            if (!rules.IpRules.Any(r => r.IPAddressOrRange.Equals(currentIp)))
            {
                _logger.LogInformation("Updating with current IP address: {currentIp}", currentIp);

                var updateParam = new StorageAccountUpdateParameters
                {
                    NetworkRuleSet = new NetworkRuleSet
                    {
                        DefaultAction = DefaultAction.Deny,
                        ResourceAccessRules = new List<ResourceAccessRule>(),
                        VirtualNetworkRules = new List<VirtualNetworkRule>(),
                        IpRules = new List<IPRule> { new IPRule(currentIp, Action.Allow) },
                        Bypass = "Logging, Metrics"
                    }
                };

                var rulesResponse = await storClient.StorageAccounts.UpdateAsync(
                    _firewallConfig.ResourceGroupName,
                    _firewallConfig.AccountName,
                    updateParam,
                    cancellationToken);
            }
            else
            {
                _logger.LogInformation("No need to update firewall rules, current IP address already present.");
            }
        }
    }
}
