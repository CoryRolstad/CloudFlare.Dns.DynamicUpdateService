CloudFlare.Dns.DynamicUpdateService 
----------------------------------------------
A small service that updates your CloudFlare DNS Records to your external IP Address. 

Things to keep in mind
----------------------------------------------
You must wrap the calls in error handling, the http call will throw errors on non success HTTP status codes. 

Logging
--------
The library offers logging capabilities.

Configuration Format (appsettings.json)
----------------------------------------
IpServiceSettings.TimeoutInSeconds is the HTTPClient timeout used when getting the ipaddress initially
DynamicUpdateSettings.SecondsBetweenChecks is the time in seconds between when the service runs
DynamicUpdateSettings.CloudFlareApiToken is your CloudFlare Token - this is required 
DynamicUpdateSettings.DnsNamesToCheck is a comma seperated list of DNS ARecords to check(and will be updated if different then the local external IP Address)

```
...
  "IpServiceSettings": {
    "TimeOutInSeconds": 30
  },
  "DynamicUpdateSettings": {
    "SecondsBetweenChecks": 20,
    "CloudFlareApiToken": "",
    "DnsNamesToCheck": "example.com, testingan.example.com"
  }
}

```
