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
            var path = context.Request.Path.ToString();

            #region Verificación de acceso
            if (!IsPathMatch(path))
            {
                await _next(context);
                return;
            }

            if (!IsAuthorized(context))
            {
                return;
            }
            #endregion

            int currentDb = GetCurrentDb(context);
            var redisDb = RedisConnectionFactory.Connection.GetDatabase(currentDb);
            var (dbSize, keyspaces) = await ServerHelper.GetDbInfo(redisDb);

            var layoutModel = new LayoutModel
            {
                DbList = keyspaces.Select(x => x.Db).ToList(),
                CurrentDb = currentDb,
                DbSize = dbSize.ToString()
            };

            var routeHandlers = new (Func<string, bool> PathMatch, string Method, Func<Task> Handler)[]
            {
                // Estadísticas
                (
                    IsStatisticsRequest,
                    HttpMethods.Get,
                    () => RenderStatistics(context, redisDb, keyspaces, layoutModel)
                ),
                // Índice
                (
                    IsIndexRequest,
                    HttpMethods.Get,
                    () => RenderIndex(context, layoutModel)
                ),
                // logout
                (
                    p => p == $"{_settings.Path}/logout",
                    HttpMethods.Get,
                    () => HandleLogout(context)
                ),
                // Obtener claves
                (
                    p => p == $"{_settings.Path}/keys",
                    HttpMethods.Get,
                    () => HandleGetKeys(context, redisDb)
                ),
                // Obtener clave específica
                (
                    p => p.StartsWith($"{_settings.Path}/keys/"),
                    HttpMethods.Get,
                    async () =>
                    {
                        var key = Uri.UnescapeDataString(path[(($"{_settings.Path}/keys/").Length)..]);
                        await HandleGetKey(context, redisDb, key);
                    }
                ),
                // Crear/Actualizar clave
                (
                    p => p.StartsWith($"{_settings.Path}/keys/"),
                    HttpMethods.Put,
                    () => HandleSetKey(context, redisDb)
                ),
                (
                    p => p.StartsWith($"{_settings.Path}/keys"),
                    HttpMethods.Post,
                    () => HandleSetKey(context, redisDb)
                ),
                // Eliminar clave
                (
                    p => p.StartsWith($"{_settings.Path}/keys/"),
                    HttpMethods.Delete,
                    async () =>
                    {
                        var key = Uri.UnescapeDataString(path[(($"{_settings.Path}/keys/").Length)..]);
                        await HandleDeleteKey(context, redisDb, key);
                    }
                ),
                (
                    p => p == $"{_settings.Path}/delete-by-pattern",
                    HttpMethods.Post,
                    () => HandleDeleteByPattern(context, redisDb)
                ),
                (
                    p => p == $"{_settings.Path}/bulk-operation",
                    HttpMethods.Post,
                    () => HandleBulkOperation(context, redisDb)
                ),
            };

            foreach (var (PathMatch, Method, Handler) in routeHandlers)
            {
                if (PathMatch(path) && context.Request.Method == Method)
                {
                    await Handler();
                    return;
                }
            }

            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }

        private static async Task HandleLogout(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"Redis Dashboard\"";
            await context.Response.WriteAsync("Logged out.");
        }


        private static async Task HandleGetKeys(HttpContext context, IDatabase redisDb)
        {
            var query = new RequestQueryParamsModel(context.Request);
            var (skeys, nextCursor) = await ServerHelper.ScanKeys(redisDb, query);
            var keys = skeys.Select(x => new KeyModel { Name = x }).ToList();
            await ServerHelper.ResolveKeyDetails(redisDb, keys);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                keys = keys,
                cursor = nextCursor
            });
        }

        private static async Task HandleGetKey(HttpContext context, IDatabase redisDb, string key)
        {
            var type = await redisDb.KeyTypeAsync(key);
            var value = await RedisValueHelper.GetValue(redisDb, key, type);
            var ttl = await redisDb.KeyTimeToLiveAsync(key);
            var length = await RedisValueHelper.GetLength(value);
            var badge = RedisValueHelper.GetBadge(type);

            var model = new KeyModel
            {
                Name = key,
                KeyType = type,
                Detail = new RedisKeyDetails
                {
                    Value = value,
                    Length = length,
                    TTL = ttl?.TotalSeconds,
                    Badge = badge
                }
            };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(model);
        }

        private static async Task HandleSetKey(HttpContext context, IDatabase redisDb)
        {
            var model = await JsonSerializer.DeserializeAsync<KeyInputModel>(context.Request.Body,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            await RedisValueHelper.SetValue(redisDb, model);
            context.Response.StatusCode = StatusCodes.Status201Created;
        }

        private static async Task HandleDeleteKey(HttpContext context, IDatabase redisDb, string key)
        {
            await redisDb.KeyDeleteAsync(key);
            context.Response.StatusCode = StatusCodes.Status204NoContent;
        }

        private static async Task HandleDeleteByPattern(HttpContext context, IDatabase redisDb)
        {
            var query = new RequestQueryParamsModel(context.Request);

            // Flush mode
            if (context.Request.Query.TryGetValue("flush", out var flushValue) && flushValue == "true")
            {
                await redisDb.ExecuteAsync("FLUSHDB");
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsync("Database flushed.");
                return;
            }

            if (string.IsNullOrWhiteSpace(query.SearchKey))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Missing required pattern (SearchKey).");
                return;
            }

            var (keysToDelete, _) = await ServerHelper.ScanKeys(redisDb, query);

            if (!keysToDelete.Any())
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }

            var deletedCount = await ServerHelper.DeleteKeys(redisDb, keysToDelete);

            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync($"{deletedCount} keys deleted.");
        }

        private static async Task HandleBulkOperation(HttpContext context, IDatabase redisDb)
        {
            var operationRequest = await JsonSerializer.DeserializeAsync<BulkOperationModel>(
                context.Request.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (operationRequest == null || string.IsNullOrEmpty(operationRequest.Operation) || operationRequest.Keys == null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid request.");
                return;
            }

            var user = context.User?.Identity?.Name ?? "anonymous";
            var timestamp = DateTime.UtcNow;

            switch (operationRequest.Operation.ToLower())
            {
                case "delete":
                    var deleted = await ServerHelper.BulkDeleteKeys(redisDb, operationRequest.Keys);
                    Console.WriteLine($"[Bulk-Delete] {deleted} keys deleted by {user} at {timestamp}");
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync($"{deleted} keys deleted.");
                    break;

                case "expire":
                    if (operationRequest.Args == null || !int.TryParse(operationRequest.Args.ToString(), out int ttl))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Missing or invalid TTL in 'Args'.");
                        return;
                    }
                    var expired = await ServerHelper.BulkExpireKeys(redisDb, operationRequest.Keys, ttl);
                    Console.WriteLine($"[Bulk-Expire] {expired} keys set to expire in {ttl}s by {user} at {timestamp}");
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync($"{expired} keys updated with TTL.");
                    break;

                case "rename":
                    if (operationRequest.Args is not JsonElement el || !el.TryGetProperty("prefix", out var prefixElement))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Missing prefix in 'Args'.");
                        return;
                    }
                    string prefix = prefixElement.GetString();
                    var renamed = await ServerHelper.BulkRenameKeys(redisDb, operationRequest.Keys, prefix);
                    Console.WriteLine($"[Bulk-Rename] {renamed} keys renamed with prefix '{prefix}' by {user} at {timestamp}");
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync($"{renamed} keys renamed.");
                    break;

                default:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Unsupported operation.");
                    break;
            }
        }


        private bool IsPathMatch(string path) =>
            path.ToString().StartsWith(_settings.Path);

        private bool IsAuthorized(HttpContext context) =>
            _settings.AuthorizationFilter == null || _settings.AuthorizationFilter.Authorize(context);

        private bool IsStatisticsRequest(string path) =>
            path == $"{_settings.Path}/statistics";

        private bool IsIndexRequest(string path) =>
            (path.ToString() == _settings.Path || path.ToString() == _settings.Path + "/");

        private static int GetCurrentDb(HttpContext context)
        {
            if (context.Request.Query.TryGetValue("db", out var dbValue) && int.TryParse(dbValue, out var parsedDb))
                return parsedDb;
            return 0;
        }

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
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(Layout.Build(layoutModel, _settings));
        }

        private async Task RenderIndex(HttpContext context, LayoutModel layoutModel)
        {
            layoutModel.Section = Main.BuildBase(_settings);
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(Layout.Build(layoutModel, _settings));
        }
    }
}
