using Microsoft.AspNetCore.Http;

namespace RedisUI.Models
{
    /// <summary>
    /// Represents query parameters extracted from an HTTP request for Redis UI pagination and filtering.
    /// </summary>
    public readonly struct RequestQueryParamsModel
    {
        public int Db { get; }
        public long Page { get; }
        public int PageSize { get; }
        public string SearchKey { get; }

        public RequestQueryParamsModel(HttpRequest request)
        {
            var query = request.Query;

            Db = ParseInt(query, "db", 0);
            Page = ParseLong(query, "page", 0);
            PageSize = ParseInt(query, "size", 10);
            SearchKey = query.TryGetValue("key", out var keyVal) ? keyVal.ToString() : string.Empty;
        }

        private static int ParseInt(IQueryCollection query, string key, int defaultValue)
        {
            return query.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
                ? parsed
                : defaultValue;
        }

        private static long ParseLong(IQueryCollection query, string key, long defaultValue)
        {
            return query.TryGetValue(key, out var value) && long.TryParse(value, out var parsed)
                ? parsed
                : defaultValue;
        }
    }
}
