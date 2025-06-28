using Microsoft.AspNetCore.Http;

namespace RedisUI.Filters
{
    public class DashboardJwtAuthorizationFilter : IRedisAuthorizationFilter
    {
        private readonly string _requiredRole;

        public DashboardJwtAuthorizationFilter(string requiredRole)
        {
            _requiredRole = requiredRole;
        }

        public bool Authorize(HttpContext context)
        {
            var user = context.User;
            return user.Identity?.IsAuthenticated == true &&
                   user.IsInRole(_requiredRole);
        }
    }

}
