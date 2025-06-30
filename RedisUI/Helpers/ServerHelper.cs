using RedisUI.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedisUI.Helpers
{
    public static class ServerHelper
    {
        public static async Task<(object dbSize, List<KeyspaceModel> keyspaces)> GetDbInfo(IDatabase redisDb)
        {
            var dbSizeTask = redisDb.ExecuteAsync("DBSIZE");
            var keyspaceTask = redisDb.ExecuteAsync("INFO", "KEYSPACE");
            await Task.WhenAll(dbSizeTask, keyspaceTask);

            var keyspaces = keyspaceTask.Result
                .ToString()
                .Replace("# Keyspace", "")
                .Split(new string[] { "\r\n" }, StringSplitOptions.None)
                .Where(item => !string.IsNullOrEmpty(item))
                .Select(KeyspaceModel.Instance)
                .ToList();

            return (dbSizeTask.Result, keyspaces);
        }

        public static async Task<(List<KeyModel> keys, long nextCursor)> ScanKeys(IDatabase redisDb, RequestQueryParamsModel queryParams)
        {
            const int MAX_SCAN_LIMIT = 10_000;
            var allMatches = new List<string>();
            long scanCursor = queryParams.Cursor;
            long nextCursor = 0;

            if (string.IsNullOrEmpty(queryParams.SearchKey))
            {
                var result = await redisDb.ExecuteAsync("SCAN", scanCursor.ToString(), "COUNT", queryParams.PageSize.ToString());
                var innerResult = (RedisResult[])result;
                scanCursor = long.Parse((string)innerResult[0]);
                allMatches = ((string[])innerResult[1]).ToList();
                nextCursor = scanCursor;
            }
            else
            {
                do
                {
                    var result = await redisDb.ExecuteAsync("SCAN", scanCursor.ToString(), "MATCH", queryParams.SearchKey, "COUNT", 1_000);
                    var innerResult = (RedisResult[])result;
                    scanCursor = long.Parse((string)innerResult[0]);
                    var partial = (string[])innerResult[1];
                    allMatches.AddRange(partial);

                    if (allMatches.Count >= queryParams.PageSize || scanCursor == 0 || allMatches.Count >= MAX_SCAN_LIMIT)
                        break;
                } while (true);

                nextCursor = scanCursor;
            }

            var pagedKeys = allMatches.Take(queryParams.PageSize).ToList();
            var keys = pagedKeys.Select(x => new KeyModel { Name = x }).ToList();
            return (keys, nextCursor);
        }

        public static async Task ResolveKeyDetails(IDatabase redisDb, List<KeyModel> keys)
        {
            int maxConcurrency = Math.Max(1, Environment.ProcessorCount);
            var semaphore = new SemaphoreSlim(maxConcurrency);
            await Parallel.ForEachAsync(keys, async (key, ct) =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    key.KeyType = await redisDb.KeyTypeAsync(key.Name);
                    key.Detail = await RedisKeyValueResolver.ResolveDetailedAsync(redisDb, key.Name, key.KeyType);
                }
                finally
                {
                    semaphore.Release();
                }
            });
        }
    }
}
