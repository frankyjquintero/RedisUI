using RedisUI.Models;
using RedisUI.Helpers;
using System.Linq;
using System.Collections.Generic;

namespace RedisUI.Pages
{
        public static class Statistics
        {
            public static string Build(StatisticsVm model)
            {
                string Table(string icon, string title, string[] headers, IEnumerable<string[]> rows) =>
                    $@"<div class=""col""><table class=""table table-hover""><thead><tr class=""table-active""><th colspan=""{headers.Length}""><span>{icon}</span>{title}</th></tr><tr>{string.Join("", headers.Select(h => $@"<th scope=""col"">{h}</th>"))}</tr></thead><tbody>{string.Join("", rows.Select(r => $"<tr>{string.Join("", r.Select(c => $"<td>{c}</td>"))}</tr>"))}</tbody></table></div>";

                var keyspaceRows = model.Keyspaces.Select(k => new[] { k.Db, k.Keys, k.Expires, k.Avg_Ttl });
                var infoRows = model.AllInfo.Select(i => new[] { i.Key, i.Value });

                return $@"
                <div class=""row"">
                    {Card("<i class=\"bi bi-hdd-network\"></i>", "Server", new[] {
                        $"Redis Version: <strong>{model.Server.RedisVersion}</strong>",
                        $"Redis Mode: <strong>{model.Server.RedisMode}</strong>",
                        $"TCP Port: <strong>{model.Server.TcpPort}</strong>"
                    })}
                    {Card("<i class=\"bi bi-memory\"></i>", "Memory", new[] {
                        $"Used Memory: <strong>{model.Memory.UsedMemory.ToMegabytes()}</strong>M",
                        $"Used Memory Peak: <strong>{model.Memory.UsedMemoryPeak.ToMegabytes()}</strong>M",
                        $"Used Memory Lua: <strong>{model.Memory.UsedMemoryLua.ToMegabytes()}</strong>M"
                    })}
                    {Card("<i class=\"bi bi-bar-chart\"></i>", "Stats", new[] {
                        $"Total Connections Received: <strong>{model.Stats.TotalConnectionsReceived}</strong>",
                        $"Total Commands Processed: <strong>{model.Stats.TotalCommandsProcessed}</strong>",
                        $"Expired Keys: <strong>{model.Stats.ExpiredKeys}</strong>"
                    })}
                </div>
                <div class=""row"">
                    {Table("<i class=\"bi bi-key-fill\"></i>", "Key Statistics", new[] { "DB", "Keys", "Expires", "Avg Ttl" }, keyspaceRows)}
                </div>
                <div class=""row"">
                    {Table("<i class=\"bi bi-info-circle\"></i>", "All Information", new[] { "Key", "Value" }, infoRows)}
                </div>";

        }

        private static string Card(string icon, string title, string[] items) =>
                $@"<div class=""col-4""><div class=""card border-info mb-3 sticky-top""><div class=""card-header""><strong><span>{icon}</span> {title}</strong></div><div class=""card-body""><ul class=""list-group list-group-flush"">{string.Join("", items.Select(i => $@"<li class=""list-group-item"">{i}</li>"))}</ul></div></div></div>";
        }
}
