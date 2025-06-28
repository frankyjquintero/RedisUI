using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace RedisUI.Filters
{
    public class DashboardIpWhitelistAuthorizationFilter : IRedisAuthorizationFilter
    {
        private readonly HashSet<string> _allowedIps;

        public DashboardIpWhitelistAuthorizationFilter(IEnumerable<string> allowedIps)
        {
            _allowedIps = new HashSet<string>(allowedIps);
        }

        public bool Authorize(HttpContext context)
        {
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            return remoteIp != null && _allowedIps.Contains(remoteIp);
        }
    }

}
