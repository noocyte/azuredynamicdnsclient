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

namespace DynamicDnsClient.Firewall;

public class FirewallRulesUpdater(
    IOptions<SecurityConfig> securityConfig,
    IOptions<FirewallConfig> firewallConfig,
    IGetCurrentIpAddress ipAddressLookup,
    ILogger<FirewallRulesUpdater> logger)
{
    private readonly SecurityConfig _securityConfig = securityConfig.Value;
    private readonly FirewallConfig _firewallConfig = firewallConfig.Value;

    public async Task UpdateFirewall(CancellationToken cancellationToken)
    {
        var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(_securityConfig.TenantId, _securityConfig.ClientId, _securityConfig.ClientSecret);
        var currentIp = await ipAddressLookup.GetCurrentIdAsync(cancellationToken);

        var storClient = new StorageManagementClient(serviceCreds)
        {
            SubscriptionId = _securityConfig.SubscriptionId
        };

        var sProp = storClient.StorageAccounts.GetProperties(_firewallConfig.ResourceGroupName, _firewallConfig.AccountName);
        var rules = sProp.NetworkRuleSet;
        var currentIpAddresses = string.Join(',', rules.IpRules.Select(r => r.IPAddressOrRange));
        logger.LogInformation("Current allowed IP addresses: {currentIpAddesses}", currentIpAddresses);

        if (!rules.IpRules.Any(r => r.IPAddressOrRange.Equals(currentIp)))
        {
            logger.LogInformation("Updating with current IP address: {currentIp}", currentIp);

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
            logger.LogInformation("No need to update firewall rules, current IP address already present.");
        }
    }
}
