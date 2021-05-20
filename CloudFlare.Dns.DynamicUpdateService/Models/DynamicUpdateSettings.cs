namespace CloudFlare.Dns.DynamicUpdateService.Models
{
    public class DynamicUpdateSettings
    {
        public int SecondsBetweenChecks { get; set; }
        public string CloudFlareApiToken { get; set; }
        public string DnsNamesToCheck { get; set; }
    }
}
