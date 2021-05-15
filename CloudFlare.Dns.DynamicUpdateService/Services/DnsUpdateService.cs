using CloudFlare.Dns.DynamicUpdateService.Models;
using Ipify.GetMyIpAddress;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CloudFlare.Dns.DynamicUpdateService.Services
{
    public class DnsUpdateService : BackgroundService
    {
        private ILogger<DnsUpdateService> _logger;
        private IIpService _ipService;
        private IPAddress _previousCheckIpAddress;
        private DynamicUpdateSettings _dynamicUpdateSettings; 
        public DnsUpdateService(ILogger<DnsUpdateService> logger, IIpService ipService, IOptions<DynamicUpdateSettings> dynamicUpdateSettings)
        {
            _logger = logger ?? throw new ArgumentNullException($"{nameof(logger)} cannot be null");
            _ipService = ipService ?? throw new ArgumentNullException($"{nameof(ipService)} cannot be null");
            if(dynamicUpdateSettings == null)
                throw new ArgumentNullException($"{nameof(dynamicUpdateSettings)} cannot be null");
            if(dynamicUpdateSettings.Value == null)
                throw new ArgumentNullException($"{nameof(dynamicUpdateSettings)} cannot be null");
            _dynamicUpdateSettings = dynamicUpdateSettings.Value;
        }


        public async Task UpdateDns()
        {
            IPAddress ipAddress;

            // Get IP Address, on intiailize assign _previousCheckIpAddress
            try
            {
                ipAddress = await GetExternalIPv4();
                if(_previousCheckIpAddress == null)
                {
                    _previousCheckIpAddress = ipAddress;
                }
            }
            catch (WebException ex)
            {
                _logger.LogError($"{typeof(WebException)} was thrown, {ex.Message}");
                return; 
            }
            
            // Check to see if the previous ip check is the same as the new one
            if(_previousCheckIpAddress.Equals(ipAddress))
            {
                _logger.LogInformation($"IP Address is the same as previous check {ipAddress.ToString()}");
                return; 
            }
            else
            {
                // Run Cloudflare DNS Update Procdure
            }

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Worker running at: {DateTimeOffset.Now}");

                await Task.Delay(_dynamicUpdateSettings.SecondsBetweenChecks * 1000, stoppingToken);
            }
        }

        private async Task<IPAddress> GetExternalIPv4()
        {
            return await _ipService.GetExternalIpv4(); 
        }

    }
}
