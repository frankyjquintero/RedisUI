using Microsoft.AspNetCore.Http;
using RedisUI.Helpers;
using RedisUI.Infra;
using RedisUI.Models;
using RedisUI.Pages;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RedisUI
{
    public class RedisUIMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RedisUISettings _settings;

        public RedisUIMiddleware(RequestDelegate next, RedisUISettings settings)
        {
            _next = next;
            _settings = settings;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!IsPathMatch(context))
            {
                await _next(context);
                return;
            }

            if (!IsAuthorized(context))
            {
                return;
            }

            int currentDb = RedisUIMiddleware.GetCurrentDb(context);
            var redisDb = RedisConnectionFactory.Connection.GetDatabase(currentDb);

            var (dbSize, keyspaces) = await RedisUIMiddleware.GetDbInfo(redisDb);

            var layoutModel = new LayoutModel
            {
                DbList = keyspaces.Select(x => x.Db).ToList(),
                CurrentDb = currentDb,
                DbSize = dbSize.ToString()
            };

            if (IsStatisticsRequest(context))
            {
                await RenderStatistics(context, redisDb, keyspaces, layoutModel);
                return;
            }

            var queryParams = new RequestQueryParamsModel(context.Request);

            if (HttpMethods.IsPost(context.Request.Method))
                await RedisUIMiddleware.ProcessPostBody(context, redisDb);

            var (keys, nextCursor) = await ScanKeys(redisDb, queryParams);

            await RedisUIMiddleware.ResolveKeyDetails(redisDb, keys);

            layoutModel.Section = Main.Build(keys, nextCursor);
            await context.Response.WriteAsync(Layout.Build(layoutModel, _settings));
        }

        private bool IsPathMatch(HttpContext context) =>
            context.Request.Path.ToString().StartsWith(_settings.Path);

        private bool IsAuthorized(HttpContext context) =>
            _settings.AuthorizationFilter == null || _settings.AuthorizationFilter.Authorize(context);

        private static int GetCurrentDb(HttpContext context)
        {
            if (context.Request.Query.TryGetValue("db", out var dbValue) && int.TryParse(dbValue, out var parsedDb))
                return parsedDb;
            return 0;
        }

        private static async Task<(object dbSize, List<KeyspaceModel> keyspaces)> GetDbInfo(IDatabase redisDb)
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

        private bool IsStatisticsRequest(HttpContext context) =>
            context.Request.Path.ToString() == $"{_settings.Path}/statistics";

        private async Task RenderStatistics(HttpContext context, IDatabase redisDb, List<KeyspaceModel> keyspaces, LayoutModel layoutModel)
        {
            var serverTask = redisDb.ExecuteAsync("INFO", "SERVER");
            var memoryTask = redisDb.ExecuteAsync("INFO", "MEMORY");
            var statsTask = redisDb.ExecuteAsync("INFO", "STATS");
            var allTask = redisDb.ExecuteAsync("INFO");

            await Task.WhenAll(serverTask, memoryTask, statsTask, allTask);

            var model = new StatisticsVm
            {
                Keyspaces = keyspaces,
                Server = ServerModel.Instance(serverTask.Result.ToString()),
                Memory = MemoryModel.Instance(memoryTask.Result.ToString()),
                Stats = StatsModel.Instance(statsTask.Result.ToString()),
                AllInfo = allTask.Result.ToString().ToInfo()
            };

            layoutModel.Section = Statistics.Build(model);
            await context.Response.WriteAsync(Layout.Build(layoutModel, _settings));
        }

        private static async Task ProcessPostBody(HttpContext context, IDatabase redisDb)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body)) return;

            if (JsonSerializer.Deserialize<PostModel>(body) is not { } postModel) return;

            if (!string.IsNullOrWhiteSpace(postModel.DelKey))
                await redisDb.KeyDeleteAsync(postModel.DelKey);

            if (!string.IsNullOrWhiteSpace(postModel.InsertKey) &&
                !string.IsNullOrWhiteSpace(postModel.InsertValue))
            {
                await redisDb.StringSetAsync(postModel.InsertKey, postModel.InsertValue);
            }
        }

        private async Task<(List<KeyModel> keys, long nextCursor)> ScanKeys(IDatabase redisDb, RequestQueryParamsModel queryParams)
        {
            const int MAX_SCAN_LIMIT = 1_000_000;
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
                    var result = await redisDb.ExecuteAsync("SCAN", scanCursor.ToString(), "MATCH", queryParams.SearchKey, "COUNT", 1000);
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

        private static async Task ResolveKeyDetails(IDatabase redisDb, List<KeyModel> keys)
        {
            int maxConcurrency = Math.Max(1, Environment.ProcessorCount);
            var semaphore = new SemaphoreSlim(maxConcurrency);
            await Parallel.ForEachAsync(keys, async (key, ct) =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    key.KeyType = await redisDb.KeyTypeAsync(key.Name);
                    key.Detail = await RedisKeyValueResolver.ResolveDetailedAsync(redisDb, key.Name);
                }
                finally
                {
                    semaphore.Release();
                }
            });
        }
    }
}
