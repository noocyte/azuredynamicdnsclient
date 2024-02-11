namespace DynamicDnsClient.Cloudflare;

internal class CloudflareConfig
{
    public string ApiKey { get; set; } = "";
    public string Identifier { get; set; } = "";
    public string[] IpApplications { get; set; } = [];
}
