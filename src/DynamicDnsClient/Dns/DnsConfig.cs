using System;

namespace DynamicDnsClient.Dns
{
    public class DnsConfig
    {
        [Obsolete]
        public string SubscriptionId { get; set; }
        public string ResourceGroupName { get; set; }
        public string ZoneName { get; set; }
        public string[] RecordSetNames { get; set; }
    }
}
