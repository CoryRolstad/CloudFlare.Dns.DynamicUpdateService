using CloudFlare.Dns.DynamicUpdateService.Models;
using CloudFlare.Dns.DynamicUpdateService.Services;
using Ipify.GetMyIpAddress;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace CloudFlare.Dns.DynamicUpdateService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.AddJsonFile("hostsettings.json", optional: true);
                    configHost.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    services.AddIpService(hostContext.Configuration.GetSection("IpServiceSettings").Get<IpServiceSettings>());
                    services.Configure<DynamicUpdateSettings>(hostContext.Configuration.GetSection("DynamicUpdateSettings"));
                    services.AddHostedService<DnsUpdateService>();
                });
    }
}
