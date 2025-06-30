using RedisUI.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedisUI.Helpers
{
    public static class RedisValueHelper
    {
        public static async Task<dynamic> GetValue(IDatabase db, string key, RedisType type)
        {
            switch (type)
            {
                case RedisType.String:
                    return (await db.StringGetAsync(key)).ToString();

                case RedisType.List:
                    var list = await db.ListRangeAsync(key);
                    return list.Select(v => (string)v).ToList();

                case RedisType.Set:
                    var set = await db.SetMembersAsync(key);
                    return set.Select(v => (string)v).ToList();

                case RedisType.SortedSet:
                    var sorted = await db.SortedSetRangeByRankWithScoresAsync(key);
                    return sorted.Select(x => new { value = (string)x.Element, score = x.Score }).ToList();

                case RedisType.Hash:
                    var hash = await db.HashGetAllAsync(key);
                    return hash.ToDictionary(h => (string)h.Name, h => (string)h.Value);

                case RedisType.Stream:
                    var stream = await db.StreamReadAsync(key, "0-0");
                    return stream.Select(entry => new
                    {
                        id = entry.Id.ToString(),
                        values = entry.Values.ToDictionary(x => (string)x.Name, x => (string)x.Value)
                    }).ToList();

                default:
                    return $"[Unsupported type: {type}]";
            }
        }

        public static async Task<double> GetLength(object value)
        {
            if (value is null) return 0;

            var json = JsonSerializer.Serialize(value);
            var bytes = System.Text.Encoding.UTF8.GetByteCount(json);
            return bytes / 1024.0; // KB
        }

        public static string GetBadge(RedisType type)
        {
            return type switch
            {
                RedisType.String => "bg-primary",
                RedisType.List => "bg-success",
                RedisType.Set => "bg-warning text-dark",
                RedisType.SortedSet => "bg-info text-dark",
                RedisType.Hash => "bg-danger",
                RedisType.Stream => "bg-secondary",
                _ => "bg-dark"
            };
        }

        private static readonly Dictionary<string, RedisType> _fromString = new(StringComparer.OrdinalIgnoreCase)
        {
            ["string"] = RedisType.String,
            ["list"] = RedisType.List,
            ["set"] = RedisType.Set,
            ["sortedset"] = RedisType.SortedSet,
            ["hash"] = RedisType.Hash,
            ["stream"] = RedisType.Stream
        };

        public static RedisType Parse(string input) =>
            _fromString.TryGetValue(input, out var type) ? type : RedisType.Unknown;


        public static async Task SetValue(IDatabase db, KeyInputModel model)
        {
            var key = model.Name;
            var value = model.Value;
            var type = Parse(model.KeyType);

            switch (type)
            {
                case RedisType.String:
                    await db.StringSetAsync(key, value?.ToString());
                    break;

                case RedisType.List:
                    await db.KeyDeleteAsync(key);
                    var list = JsonSerializer.Deserialize<List<string>>(value.ToString() ?? "[]");
                    foreach (var item in list)
                        await db.ListRightPushAsync(key, item);
                    break;

                case RedisType.Set:
                    await db.KeyDeleteAsync(key);
                    var set = JsonSerializer.Deserialize<List<string>>(value.ToString() ?? "[]");
                    foreach (var item in set)
                        await db.SetAddAsync(key, item);
                    break;

                case RedisType.SortedSet:
                    await db.KeyDeleteAsync(key);
                    var sorted = JsonSerializer.Deserialize<List<SortedItem>>(value.ToString() ?? "[]");
                    foreach (var item in sorted)
                        await db.SortedSetAddAsync(key, item.Value, item.Score);
                    break;

                case RedisType.Hash:
                    await db.KeyDeleteAsync(key);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(value.ToString() ?? "{}");
                    foreach (var kv in dict)
                        await db.HashSetAsync(key, kv.Key, kv.Value);
                    break;

                case RedisType.Stream:
                    await db.KeyDeleteAsync(key);
                    var entries = JsonSerializer.Deserialize<List<StreamEntryModel>>(value?.ToString() ?? "[]");
                    if (entries != null && entries.Count > 0)
                    {
                        foreach (var entry in entries)
                        {
                            if (entry?.Values == null || entry.Values.Count == 0) continue;
                            var nameValueEntries = entry.Values
                                .Select(kv => new NameValueEntry((RedisValue)kv.Key, (RedisValue)kv.Value))
                                .ToArray();
                            if (nameValueEntries.Length > 0)
                                await db.StreamAddAsync(key, nameValueEntries);
                        }
                    }
                    break;
            }
        }
    }


    public class SortedItem
    {
        public string Value { get; set; }
        public double Score { get; set; }
    }

    public class StreamEntryModel
    {
        public string Id { get; set; }
        public Dictionary<string, string> Values { get; set; }
    }
}
