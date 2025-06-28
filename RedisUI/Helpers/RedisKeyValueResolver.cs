using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RedisUI.Helpers
{
    public static class RedisKeyValueResolver
    {
        public static async Task<(string Value, string Badge)> ResolveAsync(IDatabase db, string key)
        {
            var type = await db.KeyTypeAsync(key);

            return type switch
            {
                RedisType.String => (await db.StringGetAsync(key), "light"),

                RedisType.Hash => (
                    string.Join(", ", (await db.HashGetAllAsync(key))
                        .Select(x => $"{x.Name}: {x.Value}")),
                    "success"),

                RedisType.List => (
                    string.Join(", ", await db.ListRangeAsync(key)),
                    "warning"),

                RedisType.Set => (
                    string.Join(", ", await db.SetMembersAsync(key)),
                    "primary"),

                RedisType.None => (await db.StringGetAsync(key), "secondary"),

                _ => ("Unsupported Type", "dark")
            };
        }
    }
}