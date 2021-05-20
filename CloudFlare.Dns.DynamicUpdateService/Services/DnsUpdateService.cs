using CloudFlare.Client;
using CloudFlare.Client.Api.Authentication;
using CloudFlare.Dns.DynamicUpdateService.Models;
using Ipify.GetMyIpAddress;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using CloudFlare.Client.Api.Zones.DnsRecord;
using CloudFlare.Client.Enumerators;

namespace CloudFlare.Dns.DynamicUpdateService.Services
{
    public class DnsUpdateService : BackgroundService
    {
        private ILogger<DnsUpdateService> _logger;
        private IIpService _ipService;
        private DynamicUpdateSettings _dynamicUpdateSettings;
        private readonly IAuthentication _authentication; 
        public DnsUpdateService(ILogger<DnsUpdateService> logger, IIpService ipService, IOptions<DynamicUpdateSettings> dynamicUpdateSettings)
        {
            _logger = logger ?? throw new ArgumentNullException($"{nameof(logger)} cannot be null");
            _ipService = ipService ?? throw new ArgumentNullException($"{nameof(ipService)} cannot be null");
            if (dynamicUpdateSettings == null)
                throw new ArgumentNullException($"{nameof(dynamicUpdateSettings)} cannot be null");
            if (dynamicUpdateSettings.Value == null)
                throw new ArgumentNullException($"{nameof(dynamicUpdateSettings)} cannot be null");
            _dynamicUpdateSettings = dynamicUpdateSettings.Value;
            _authentication = new ApiTokenAuthentication(_dynamicUpdateSettings.CloudFlareApiToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"\n{DateTimeOffset.Now}: DnsUpdateService Starting Execution");

                IPAddress currentIpAddress; 
                // Get External IP Address
                // TODO: Collect external ip from multiple sources to ensure we are not single threaded on depedencies
                try
                {
                    currentIpAddress = await _ipService.GetExternalIpv4();
                    _logger.LogInformation($"{DateTimeOffset.Now}: Current External IP: {currentIpAddress.ToString()}"); 
                }
                catch (WebException ex)
                {
                    _logger.LogError($"{DateTimeOffset.Now}: {typeof(WebException)} was thrown, {ex.Message}");
                    return;
                }

                // TODO: Put error handling around this split
                string[] hostnames = _dynamicUpdateSettings.DnsNamesToCheck.Split(",");
                List<string> hostnamesToUpdate = new List<string>();

                // Get ip's for the collectin of hostnames
                // TODO: Refactor
                foreach(string hostname in hostnames)
                {
                    foreach(IPAddress ip in await System.Net.Dns.GetHostAddressesAsync(hostname.Trim()))
                    {
                        _logger.LogInformation($"{DateTimeOffset.Now}: {hostname.Trim()} has an IP: {ip}");
                        if (!currentIpAddress.Equals(ip))
                        {
                            _logger.LogInformation($"{DateTimeOffset.Now}: {hostname.Trim()} is {ip.ToString()} but our local IP is {currentIpAddress.ToString()} adding {hostname.Trim()} to the list to be updated");
                            hostnamesToUpdate.Add(hostname.Trim()); 
                        }                        
                    }
                }

                if(hostnamesToUpdate.Count > 0)
                {
                    await UpdateCloudFlareDns(hostnamesToUpdate, currentIpAddress, stoppingToken);
                }
                else
                {
                    _logger.LogInformation($"{DateTimeOffset.Now}: All ARecords are up to date");
                }
                

                await Task.Delay(_dynamicUpdateSettings.SecondsBetweenChecks * 1000, stoppingToken);
            }
        }

        private async Task UpdateCloudFlareDns(List<string> hostnames, IPAddress ip, CancellationToken ct)
        {
            _logger.LogInformation($"{DateTimeOffset.Now}: Executing CloudFlare Dns updates");
            // Run Cloudflare DNS Update Procdure
            using var client = new CloudFlareClient(_authentication);

            var zones = await client.Zones.GetAsync(cancellationToken: ct);

            // TODO: Refactor foreach to be async?
            foreach (var zone in zones.Result)
            {
                var dnsRecords = await client.Zones.DnsRecords.GetAsync(zone.Id, cancellationToken: ct);
                var aRecords = dnsRecords.Result
                    .Where(x => x.Type == Client.Enumerators.DnsRecordType.A)
                    .Where(x => hostnames.Contains(x.Name))
                    .ToList();
                
                foreach(var record in aRecords)
                {
                    var modified = new ModifiedDnsRecord
                    {
                        Type = DnsRecordType.A,
                        Name = record.Name,
                        Content = ip.ToString(),
                    };
                    var updateResult = await client.Zones.DnsRecords.UpdateAsync(zone.Id, record.Id, modified, ct);

                    if (!updateResult.Success)
                    {
                        _logger.LogError($"{DateTimeOffset.Now}: The following errors happened during update of record {record.Name} in zone {zone.Name}: {updateResult.Errors}");
                    }
                    else
                    {
                        _logger.LogInformation($"{DateTimeOffset.Now}: Successfully updated record {record.Name} ip from {record.Content} to {ip.ToString()} in zone {zone.Name}");
                    }
                }

            }
        }

    }
}
