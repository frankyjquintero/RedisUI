using Microsoft.AspNetCore.Http;
using System;
using System.Text;

namespace RedisUI.Filters
{
    public class DashboardBasicAuthorizationFilter : IRedisAuthorizationFilter
    {
        private readonly string _username;
        private readonly string _password;

        public DashboardBasicAuthorizationFilter(string userName, string password)
        {
            _username = userName;
            _password = password;
        }

        public bool Authorize(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                Challenge(context);
                return false;
            }

            if (!authHeader.ToString().StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                Challenge(context);
                return false;
            }

            var encodedCredentials = authHeader.ToString()["Basic ".Length..].Trim();
            try
            {
                var decodedBytes = Convert.FromBase64String(encodedCredentials);
                var decoded = Encoding.UTF8.GetString(decodedBytes);
                var parts = decoded.Split(':', 2);
                if (parts.Length != 2)
                {
                    Challenge(context);
                    return false;
                }

                var user = parts[0];
                var pass = parts[1];

                if (user == _username && pass == _password)
                    return true;

                Challenge(context);
                return false;
            }
            catch
            {
                Challenge(context);
                return false;
            }
        }

        private static void Challenge(HttpContext context)
        {
            context.Response.StatusCode = 401;
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"Redis Dashboard\"";
        }

    }
}
