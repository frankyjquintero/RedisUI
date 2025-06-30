using RedisUI.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
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
                .Split(new[] { "\r\n" }, StringSplitOptions.None)
                .Where(item => !string.IsNullOrEmpty(item))
                .Select(KeyspaceModel.Instance)
                .ToList();

            return (dbSizeTask.Result, keyspaces);
        }

        public static async Task<(IEnumerable<string> keys, long nextCursor)> ScanKeys(IDatabase redisDb, RequestQueryParamsModel queryParams)
        {
            const int MAX_SCAN_LIMIT = 100_000;
            var allMatches = new List<string>();
            long scanCursor = queryParams.Cursor;
            int pageSize = queryParams.PageSize > MAX_SCAN_LIMIT ? MAX_SCAN_LIMIT : queryParams.PageSize;

            do
            {
                string[] args = (string.IsNullOrEmpty(queryParams.SearchKey))
                    ? new[] { scanCursor.ToString(), "COUNT", pageSize.ToString() }
                    : new[] { scanCursor.ToString(), "MATCH", queryParams.SearchKey, "COUNT", pageSize.ToString() };

                var result = await redisDb.ExecuteAsync("SCAN", args);
                var innerResult = (RedisResult[])result;
                scanCursor = long.Parse((string)innerResult[0]);
                allMatches.AddRange(((RedisResult[])result[1]).Select(x => (string)x));

                if (allMatches.Count >= pageSize || scanCursor == 0 || allMatches.Count >= MAX_SCAN_LIMIT)
                    break;
            } while (true);

            long nextCursor = scanCursor;
            var pagedKeys = allMatches.Take(pageSize);
            return (pagedKeys, nextCursor);
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

        public static async Task<int> DeleteKeys(IDatabase redisDb, IEnumerable<string> keysToDelete)
        {
            const int BatchSize = 100;
            int MaxParallelism = Math.Max(1, Environment.ProcessorCount);
            var deletedCount = 0;
            var deleteLock = new object();

            var chunks = keysToDelete.Chunk(BatchSize);

            await Parallel.ForEachAsync(chunks, new ParallelOptions { MaxDegreeOfParallelism = MaxParallelism }, async (chunk, _) =>
            {
                foreach (var keyName in chunk)
                {
                    try
                    {
                        if (await redisDb.KeyDeleteAsync(keyName))
                        {
                            lock (deleteLock)
                            {
                                deletedCount++;
                            }
                        }
                    }
                    catch (RedisConnectionException connEx)
                    {
                        Console.WriteLine("Redis connection error: " + connEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting key {keyName}: {ex.Message}");
                    }
                }
            });

            return deletedCount;
            
        }
    }
}
