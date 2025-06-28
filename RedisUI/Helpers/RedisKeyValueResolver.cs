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

                // Obtener el tamaño de la key en memoria (en bytes)
                var memResult = await db.ExecuteAsync("MEMORY", "USAGE", key);
                long sizeInBytes = memResult.IsNull ? 0 : (long)memResult;
                double sizeInKilobytes = Math.Round(sizeInBytes / 1024.0, 2); // KB con dos decimales

                RedisKeyDetails result = new()
                {
                    Type = type.ToString(),
                    TTL = ttl?.TotalSeconds is > 0 ? (long?)ttl.Value.TotalSeconds : null,
                    Badge = GetBadge(type),
                    Length = sizeInKilobytes
                };

                switch (type)
                {
                    case RedisType.String:
                        var str = await db.StringGetAsync(key);
                        result.Value = str.ToString();
                        break;

                    case RedisType.Hash:
                        var hash = (await db.HashGetAllAsync(key)).Take(limit).ToArray();
                        result.Value = hash.ToDictionary(x => x.Name.ToString(), x => (object)x.Value.ToString());
                        break;

                    case RedisType.List:
                        var list = await db.ListRangeAsync(key, 0, limit - 1);
                        result.Value = list.Select(x => x.ToString()).ToList();
                        break;

                    case RedisType.Set:
                        var set = (await db.SetMembersAsync(key)).Take(limit);
                        result.Value = set.Select(x => x.ToString()).ToList();
                        break;

                    case RedisType.SortedSet:
                        var sortedSet = await db.SortedSetRangeByRankWithScoresAsync(key, 0, limit - 1);
                        result.Value = sortedSet
                            .Select(x => new { Element = x.Element.ToString(), Score = x.Score })
                            .ToList();
                        break;

                    case RedisType.Stream:
                        var stream = await db.StreamReadAsync(key, "0-0", limit);
                        result.Value = stream.Select(e => new
                        {
                            Id = e.Id.ToString(),
                            Fields = e.Values.ToDictionary(f => f.Name.ToString(), f => (object)f.Value.ToString())
                        }).ToList();
                        break;

                    case RedisType.None:
                        result.Value = "(Key Not Found)";
                        break;

                    default:
                        result.Value = "(Unsupported or Module Type)";
                        break;
                }

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