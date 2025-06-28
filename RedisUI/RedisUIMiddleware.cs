using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using StackExchange.Redis;
using RedisUI.Pages;
using System.Linq;
using RedisUI.Models;
using RedisUI.Helpers;
using System;
using System.IO;
using System.Text.Json;
using RedisUI.Infra;

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
            RedisResult result;
            if (string.IsNullOrEmpty(queryParams.SearchKey))
            {
                result = await redisDb.ExecuteAsync("SCAN", queryParams.Page.ToString(), "COUNT", queryParams.PageSize.ToString());
            }
            else
            {
                result = await redisDb.ExecuteAsync("SCAN", queryParams.Page.ToString(), "MATCH", queryParams.SearchKey, "COUNT", queryParams.PageSize.ToString());
            }

            var innerResult = (RedisResult[])result;
            var keys = ((string[])innerResult[1])
                .Select(x => new KeyModel
                {
                    Name = x
                })
                .ToList();
            #endregion

            #region Resolución de tipos y valores
            foreach (var key in keys)
            {
                key.KeyType = await redisDb.KeyTypeAsync(key.Name);
                switch (key.KeyType)
                {
                    case RedisType.String:
                        key.Value = await redisDb.StringGetAsync(key.Name);
                        key.Badge = "light";
                        break;
                    case RedisType.Hash:
                        var hashValue = await redisDb.HashGetAllAsync(key.Name);
                        key.Value = string.Join(", ", hashValue.Select(x => $"{x.Name}: {x.Value}"));
                        key.Badge = "success";
                        break;
                    case RedisType.List:
                        var listValue = await redisDb.ListRangeAsync(key.Name);
                        key.Value = string.Join(", ", listValue.Select(x => x));
                        key.Badge = "warning";
                        break;
                    case RedisType.Set:
                        var setValue = await redisDb.SetMembersAsync(key.Name);
                        key.Value = string.Join(", ", setValue.Select(x => x));
                        key.Badge = "primary";
                        break;
                    case RedisType.None:
                        key.Value = await redisDb.StringGetAsync(key.Name);
                        key.Badge = "secondary";
                        break;
                }
            }
            #endregion

            #region Renderizado de vista principal
            layoutModel.Section = Main.Build(keys, keys.Count > 0 ? long.Parse((string)innerResult[0]) : 0);
            await context.Response.WriteAsync(Layout.Build(layoutModel, _settings));
            #endregion
        }
    }
}
