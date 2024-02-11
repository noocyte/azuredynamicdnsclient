using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicDnsClient.Cloudflare;

internal class CloudflareUpdater(
    ILogger<CloudflareUpdater> logger, 
    IOptions<CloudflareConfig> config, 
    IHttpClientFactory httpClientFactory, 
    IGetCurrentIpAddress getCurrentIpAddress)
{
    public async Task UpdateRules(CancellationToken cancellationToken)
    {
        var apikey = config.Value.ApiKey;
        var identifier = config.Value.Identifier;
        var ipApplicationIds = config.Value.IpApplications;
        var ipAddress = await getCurrentIpAddress.GetCurrentIdAsync(cancellationToken);

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apikey);
        var tokenValidationResponse = await client.GetAsync("https://api.cloudflare.com/client/v4/user/tokens/verify", cancellationToken);
        if (!tokenValidationResponse.IsSuccessStatusCode)
        {
            logger.LogError("Failed to call api, invalid token");
            return;
        }

        var accessApplications = await client.GetFromJsonAsync<Rootobject<CloudflareApp>>($"https://api.cloudflare.com/client/v4/accounts/{identifier}/access/apps", cancellationToken: cancellationToken);

        var applicationsToUpdate = accessApplications.result.Where(r => ipApplicationIds.Contains(r.id)).ToList();
        foreach (var app in applicationsToUpdate)
        {
            var appPolicies = await client.GetFromJsonAsync<Rootobject<Policy>>($"https://api.cloudflare.com/client/v4/accounts/{identifier}/access/apps/{app.uid}/policies", cancellationToken);

            var currentHomePolicy = appPolicies.result.Where(p => p.name.Equals("home", StringComparison.OrdinalIgnoreCase)).First();
            if (currentHomePolicy.include[0].ip.ip.StartsWith(ipAddress))
            {
                logger.LogInformation("Home Policy for {appid} already has corrent IP address, no need to update", app.uid);
                continue;
            }

            var newHomePolicy = new UpdateInclude { ip = new Ip { ip = ipAddress } };

            var updatedPolicy = new UpdatePolicy
            {
                id = currentHomePolicy.id,
                include = [newHomePolicy]
            };
            var serialized = JsonSerializer.Serialize(updatedPolicy);
            var updatedHomePolicyResponse = await client.PutAsJsonAsync($"https://api.cloudflare.com/client/v4/accounts/{identifier}/access/apps/{app.uid}/policies/{currentHomePolicy.uid}", updatedPolicy, cancellationToken);
            if (!updatedHomePolicyResponse.IsSuccessStatusCode)
            {
                var errorContent = await updatedHomePolicyResponse.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Failed to update policy - error: {error}", errorContent);
            }
        }
    }
}

public class UpdatePolicy
{
    public string decision { get; set; } = "bypass";
    public string id { get; set; }
    public UpdateInclude[] include { get; set; }
    public string name { get; set; } = "Home";
    public object[] require { get; set; } = [];
    public int precedence { get; set; } = 1;
}

public class UpdateInclude
{
    public Ip ip { get; set; } = new Ip();
}


public class Include
{
    public Ip ip { get; set; }
    public Group group { get; set; }
    public Everyone everyone { get; set; }
}

public class Ip
{
    public string ip { get; set; } = "";
}

public class Group
{
    public string id { get; set; }
}

public class Everyone
{
}

public class Rootobject<T>
{
    public T[] result { get; set; }
    public bool success { get; set; }
    public object[] errors { get; set; }
    public object[] messages { get; set; }
}

public class CloudflareApp
{
    public string id { get; set; }
    public string uid { get; set; }
    public string type { get; set; }
    public string name { get; set; }
    public string aud { get; set; }
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
    public string domain { get; set; }
    public string domain_type { get; set; }
    public string[] self_hosted_domains { get; set; }
    public string logo_url { get; set; }
    public bool app_launcher_visible { get; set; }
    public object[] allowed_idps { get; set; }
    public object[] tags { get; set; }
    public bool auto_redirect_to_identity { get; set; }
    public Policy[] policies { get; set; }
    public string session_duration { get; set; }
    public bool enable_binding_cookie { get; set; }
    public bool http_only_cookie_attribute { get; set; }
    public Landing_Page_Design landing_page_design { get; set; }
}

public class Landing_Page_Design
{
}

public class Policy
{
    public DateTime created_at { get; set; }
    public string decision { get; set; }
    public object[] exclude { get; set; }
    public string id { get; set; }
    public Include[] include { get; set; }
    public string name { get; set; }
    public object[] require { get; set; }
    public string uid { get; set; }
    public DateTime updated_at { get; set; }
    public int precedence { get; set; }
    public string session_duration { get; set; }
}





