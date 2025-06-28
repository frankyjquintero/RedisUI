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

                RedisType.SortedSet => (
                    string.Join(", ", (await db.SortedSetRangeByRankWithScoresAsync(key))
                        .Select(x => $"{x.Element}:{x.Score}")),
                    "info"),

                RedisType.Stream => (
                    string.Join(", ", (await db.StreamReadAsync(key, "0-0"))
                        .Select(x => $"[{x.Id}] {string.Join(", ", x.Values.Select(f => $"{f.Name}:{f.Value}"))}")),
                    "secondary"),

                // RedisType.None: Key doesn't exist, treat as empty string
                RedisType.None => ("(Key Not Found)", "secondary"),

                // For unrecognized or module-based types
                _ => ("(Unsupported or Module Type)", "dark")
            };

        }
    }
}