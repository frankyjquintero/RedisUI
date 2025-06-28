using Microsoft.AspNetCore.Http;
using System.Linq;

namespace RedisUI.Filters
{
    public class DashboardCompositeAuthorizationFilter : IRedisAuthorizationFilter
    {
        private readonly IRedisAuthorizationFilter[] _filters;

        public DashboardCompositeAuthorizationFilter(params IRedisAuthorizationFilter[] filters)
        {
            _filters = filters;
        }

        public bool Authorize(HttpContext context)
        {
            return _filters.All(filter => filter.Authorize(context));
        }
    }

}
