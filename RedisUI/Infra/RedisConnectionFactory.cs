using StackExchange.Redis;
using System;

namespace RedisUI.Infra
{
    public static class RedisConnectionFactory
    {
        private static Lazy<ConnectionMultiplexer> _lazyConnection;
        private static readonly int _timeoutDefault = 20_000;

        public static void Initialize(RedisUISettings settings)
        {
            if (_lazyConnection != null && _lazyConnection.IsValueCreated)
                return;

            _lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                ConfigurationOptions options;

                if (settings.ConfigurationOptions != null)
                {
                    options = settings.ConfigurationOptions;
                }
                else
                {
                    options = ConfigurationOptions.Parse(settings.ConnectionString);
                }

                options.ConnectTimeout = options.ConnectTimeout <= _timeoutDefault ? _timeoutDefault : options.ConnectTimeout;
                options.AsyncTimeout = options.AsyncTimeout <= _timeoutDefault ? _timeoutDefault : options.AsyncTimeout;
                options.SyncTimeout = options.SyncTimeout <= _timeoutDefault ? _timeoutDefault : options.SyncTimeout;
                options.AbortOnConnectFail = false;
                options.KeepAlive = 120;
                options.ConnectRetry = 5;

                return ConnectionMultiplexer.Connect(options);
            });
        }

        public static ConnectionMultiplexer Connection => _lazyConnection.Value;
    }


}
