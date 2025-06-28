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
            #region Ruta no coincide o no autorizado
            if (!context.Request.Path.ToString().StartsWith(_settings.Path))
            {
                await _next(context);
                return;
            }

            if (_settings.AuthorizationFilter != null && !_settings.AuthorizationFilter.Authorize(context))
            {
                context.Response.StatusCode = 403;
                return;
            }
            #endregion

            #region Conexión a Redis
            int currentDb = 0;
            if (context.Request.Query.TryGetValue("db", out var dbValue) && int.TryParse(dbValue, out var parsedDb))
            {
                currentDb = parsedDb;
            }

            var redisDb = RedisConnectionFactory.Connection.GetDatabase(currentDb);

            var dbSizeTask = redisDb.ExecuteAsync("DBSIZE");
            var keyspaceTask = redisDb.ExecuteAsync("INFO", "KEYSPACE");

            await Task.WhenAll(dbSizeTask, keyspaceTask);

            var dbSize = dbSizeTask.Result;
            var keyspace = keyspaceTask.Result;

            var keyspaces = keyspace
                .ToString()
                .Replace("# Keyspace", "")
                .Split(new string[] { "\r\n" }, StringSplitOptions.None)
                .Where(item => !string.IsNullOrEmpty(item))
                .Select(KeyspaceModel.Instance)
                .ToList();

            var layoutModel = new LayoutModel
            {
                DbList = keyspaces.Select(x => x.Db).ToList(),
                CurrentDb = currentDb,
                DbSize = dbSize.ToString()
            };

            #endregion

            #region statistics
            if (context.Request.Path.ToString() == $"{_settings.Path}/statistics")
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
                return;
            }
            #endregion


            #region Parámetros de consulta
            var queryParams = new RequestQueryParamsModel(context.Request);
            #endregion

            #region Procesamiento de cuerpo POST (inserción / eliminación)

            if (HttpMethods.IsPost(context.Request.Method))
            {
                context.Request.EnableBuffering();

                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    if (JsonSerializer.Deserialize<PostModel>(body) is { } postModel)
                    {
                        if (!string.IsNullOrWhiteSpace(postModel.DelKey))
                            await redisDb.KeyDeleteAsync(postModel.DelKey);

                        if (!string.IsNullOrWhiteSpace(postModel.InsertKey) &&
                            !string.IsNullOrWhiteSpace(postModel.InsertValue))
                        {
                            await redisDb.StringSetAsync(postModel.InsertKey, postModel.InsertValue);
                        }
                    }
                }
            }

            #endregion


            #region Escaneo de claves

            const int MAX_SCAN_LIMIT = 1_000_000;
            var allMatches = new List<string>();
            long scanCursor = queryParams.Cursor;
            long nextCursor = 0;

            
            if (string.IsNullOrEmpty(queryParams.SearchKey))
            {
                // Sin patrón de búsqueda
                var result = await redisDb.ExecuteAsync("SCAN", scanCursor.ToString(), "COUNT", queryParams.PageSize.ToString());
                var innerResult = (RedisResult[])result;

                scanCursor = long.Parse((string)innerResult[0]);
                allMatches = ((string[])innerResult[1]).ToList();
                nextCursor = scanCursor;
            }
            else
            {
                // Con patrón: escaneo acumulativo hasta tope
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

            // Corta los primeros N si se acumuló más de lo necesario
            var pagedKeys = allMatches
                .Take(queryParams.PageSize)
                .ToList();

            var keys = pagedKeys
                .Select(x => new KeyModel { Name = x })
                .ToList();

            #endregion




            #region Resolución de tipos y valores (Concurrente)

            var semaphore = new SemaphoreSlim(15);
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

            #endregion

            #region Renderizado de vista principal

            layoutModel.Section = Main.Build(keys, nextCursor);
            await context.Response.WriteAsync(Layout.Build(layoutModel, _settings));

            #endregion


        }
    }
}
