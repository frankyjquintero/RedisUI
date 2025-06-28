using StackExchange.Redis;

namespace RedisUI
{
    public class RedisUISettings
    {
        /// <summary>
        /// Gets or sets the connection string for the Redis server.
        /// </summary>
        public string ConnectionString { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the ConfigurationOptions instance.
        /// </summary>
        public ConfigurationOptions ConfigurationOptions { get; set; }

        /// <summary>
        /// Gets or sets the path for the Redis server.
        /// </summary>
        public string Path { get; set; } = "/redis";

        /// <summary>
        /// Gets or sets the Redis UI authorization filter.
        /// </summary>
        public IRedisAuthorizationFilter AuthorizationFilter { get; set; }

        /// <summary>
        /// Gets or sets the CSS link for Bootstrap.
        /// </summary>
        public string CssLink { get; set; } = "https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css";

        /// <summary>
        /// Gets or sets the JavaScript link for Bootstrap.
        /// </summary>
        public string JsLink { get; set; } = "https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js";

        /// <summary>
        /// Gets or sets the Highlight.js theme link.
        /// </summary>
        public string HighlightTheme { get; set; } = "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/monokai.min.css";

        /// <summary>
        /// Gets or sets the Highlight.js script link.
        /// </summary>
        public string HighlightJs { get; set; } = "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/highlight.min.js";

        /// <summary>
        /// Gets or sets the Highlight.js language file for JSON.
        /// </summary>
        public string HighlightJson { get; set; } = "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/json.min.js";

    }
}
