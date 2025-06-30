using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace RedisUI.Helpers
{
    public class RedisKeyDetails
    {
        public string Type { get; set; }
        public double? TTL { get; set; }
        public double? Length { get; set; }
        public object Value { get; set; }
        public string Badge { get; set; }
    }

    public static class RedisKeyValueResolver
    {
        public static async Task<RedisKeyDetails> ResolveDetailedAsync(IDatabase db, string key, RedisType type)
        {
            try
            {
                var ttl = await db.KeyTimeToLiveAsync(key);
                var memResult = await db.ExecuteAsync("MEMORY", "USAGE", key);
                double sizeInKilobytes = Math.Round((memResult.IsNull ? 0 : (long)memResult) / 1024.0, 2);

                var result = new RedisKeyDetails
                {
                    Type = type.ToString(),
                    TTL = ttl?.TotalSeconds is > 0 ? (long?)ttl.Value.TotalSeconds : null,
                    Badge = RedisValueHelper.GetBadge(type),
                    Length = sizeInKilobytes,
                    Value = await RedisValueHelper.GetValue(db, key, type)
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
    }

}