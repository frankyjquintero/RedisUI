using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace RedisUI.Filters
{
    public class DashboardEnvironmentAuthorizationFilter : IRedisAuthorizationFilter
    {
        private readonly IWebHostEnvironment _env;

        public DashboardEnvironmentAuthorizationFilter(IWebHostEnvironment env)
        {
            _env = env;
        }

        public bool Authorize(HttpContext context)
        {
            return _env.IsDevelopment() || _env.IsStaging();
        }
    }

}
