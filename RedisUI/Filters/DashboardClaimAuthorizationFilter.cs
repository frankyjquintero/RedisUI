using Microsoft.AspNetCore.Http;

namespace RedisUI.Filters
{
    public class DashboardClaimAuthorizationFilter : IRedisAuthorizationFilter
    {
        private readonly string _claimType;
        private readonly string _claimValue;

        public DashboardClaimAuthorizationFilter(string claimType, string claimValue)
        {
            _claimType = claimType;
            _claimValue = claimValue;
        }

        public bool Authorize(HttpContext context)
        {
            return context.User?.HasClaim(_claimType, _claimValue) == true;
        }
    }

}
