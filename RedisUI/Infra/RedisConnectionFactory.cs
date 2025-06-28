using StackExchange.Redis;
using System;

namespace RedisUI.Infra
{
    public static class RedisConnectionFactory
    {
        private static Lazy<ConnectionMultiplexer> _lazyConnection;

        public static void Initialize(RedisUISettings settings)
        {
            _lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                return settings.ConfigurationOptions != null
                    ? ConnectionMultiplexer.Connect(settings.ConfigurationOptions)
                    : ConnectionMultiplexer.Connect(settings.ConnectionString);
            });
        }

        public static ConnectionMultiplexer Connection => _lazyConnection.Value;
    }

}
