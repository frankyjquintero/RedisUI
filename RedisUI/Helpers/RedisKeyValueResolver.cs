using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RedisUI.Helpers
{
    public class RedisKeyDetails
    {
        public string Type { get; set; }
        public long? TTL { get; set; }
        public double? Length { get; set; }
        public object Value { get; set; }
        public string Badge { get; set; }
    }

    public static class RedisKeyValueResolver
    {
        private const int DefaultLimit = 100;

        public static async Task<RedisKeyDetails> ResolveDetailedAsync(IDatabase db, string key, int limit = DefaultLimit)
        {
            try
            {
                var type = await db.KeyTypeAsync(key);
                var ttl = await db.KeyTimeToLiveAsync(key);
                var memResult = await db.ExecuteAsync("MEMORY", "USAGE", key);
                double sizeInKilobytes = Math.Round((memResult.IsNull ? 0 : (long)memResult) / 1024.0, 2);

                var result = new RedisKeyDetails
                {
                    Type = type.ToString(),
                    TTL = ttl?.TotalSeconds is > 0 ? (long?)ttl.Value.TotalSeconds : null,
                    Badge = GetBadge(type),
                    Length = sizeInKilobytes,
                    Value = await GetValueAsync(db, key, type, limit)
                };
                return result;
            }
            catch (Exception ex)
            {
                return new RedisKeyDetails
                {
                    Type = "Error",
                    Value = $"Error: {ex.Message}",
                    Badge = "danger"
                };
            }
        }

        private static async Task<object> GetValueAsync(IDatabase db, string key, RedisType type, int limit)
        {
            return type switch
            {
                RedisType.String => (await db.StringGetAsync(key)).ToString(),
                RedisType.Hash => (await db.HashGetAllAsync(key) ?? Array.Empty<HashEntry>()).Take(limit)
                    .ToDictionary(x => x.Name.ToString(), x => (object)x.Value.ToString()),
                RedisType.List => (await db.ListRangeAsync(key, 0, limit - 1)).Select(x => x.ToString()).ToList(),
                RedisType.Set => (await db.SetMembersAsync(key)).Take(limit).Select(x => x.ToString()).ToList(),
                RedisType.SortedSet => (await db.SortedSetRangeByRankWithScoresAsync(key, 0, limit - 1))
                    .Select(x => new { Element = x.Element.ToString(), Score = x.Score }).ToList(),
                RedisType.Stream => (await db.StreamReadAsync(key, "0-0", limit)).Select(e => new
                {
                    Id = e.Id.ToString(),
                    Fields = e.Values.ToDictionary(f => f.Name.ToString(), f => (object)f.Value.ToString())
                }).ToList(),
                RedisType.None => "(Key Not Found)",
                _ => "(Unsupported or Module Type)"
            };
        }

        private static string GetBadge(RedisType type) => type switch
        {
            RedisType.String => "badge-purple",
            RedisType.Hash => "badge-blue",
            RedisType.List => "badge-green",
            RedisType.Set => "badge-orange",
            RedisType.SortedSet => "badge-magenta",
            RedisType.Stream => "badge-olive",
            RedisType.None => "badge-gray",
            _ => "badge-dark"
        };
    }

}