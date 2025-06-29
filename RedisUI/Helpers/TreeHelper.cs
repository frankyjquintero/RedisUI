using RedisUI.Contents;
using RedisUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedisUI.Helpers
{
    public static class TreeHelper
    {
        public static TreeNode Build(List<KeyModel> keys)
        {
            var root = new TreeNode { Name = "root" };

            foreach (var key in keys)
            {
                var parts = key.Name.Split(':');
                var node = root;

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    if (!node.Children.ContainsKey(part))
                        node.Children[part] = new TreeNode { Name = part };

                    node = node.Children[part];

                    // Si es la última parte del nombre, agregamos el key al nodo hoja
                    if (i == parts.Length - 1)
                        node.Keys.Add(key);
                }
            }

            return root;
        }

        public static void RenderTree(TreeNode node, StringBuilder sb, string prefix = "")
        {
            int index = 0;
            foreach (var child in node.Children.Values.OrderBy(c => c.Name))
            {
                string fullPath = (prefix == "" ? "" : prefix + ":") + child.Name;
                bool hasChildren = child.Children.Any();
                bool isLeaf = child.Keys.Any();
                string zebraClass = (index % 2 == 0) ? "bg-light" : "bg-white";
                index++;

                sb.AppendLine($@"<li class=""list-group-item p-2 {zebraClass}"">");

                if (hasChildren)
                {
                    string collapseId = "collapse_" + fullPath.Replace(":", "_");
                    bool expand = fullPath.Count(c => c == ':') <= 0;
                    string iconId = "icon_" + collapseId;

                    sb.AppendLine($@"
                    <div class=""d-flex justify-content-between align-items-center"" data-bs-toggle=""collapse"" href=""#{collapseId}"" role=""button"">
                        <a class=""fw-bold text-decoration-none text-dark"">
                            <span id=""{iconId}"" class=""me-1"">📁</span>{child.Name}
                        </a>                  
                    </div>
                    <div class=""collapse ms-3 {(expand ? "show" : "")}"" id=""{collapseId}"">
                        <ul class=""list-group list-group-flush"">");

                    RenderTree(child, sb, fullPath);

                    sb.AppendLine("</ul></div>");
                }
                else if (isLeaf)
                {
                    foreach (var key in child.Keys)
                    {
                        string badge = $"<span class='badge {key.Detail.Badge}'>{key.KeyType.ToString().ToUpper()}</span>";

                        sb.AppendLine($@"
                        <div class=""d-flex justify-content-between align-items-center"">
                            <span style=""cursor:pointer"" 
                                  class=""text-break""
                                  data-key='{key.Name}'
                                  data-value='{System.Text.Json.JsonSerializer.Serialize(key.Detail.Value)}'
                                  onclick='renderDetailPanel(JSON.parse(this.dataset.value))'>
                                🔑 {badge} {key.Name} <small class=""text-muted"">TTL: <span class=""badge bg-info"">{key.Detail.TTL?.ToString() ?? "∞"}</span>  | Size: {key.Detail.Length} KB</small>
                            </span>
                            <a onclick=""confirmDelete('{key.Name}')"" class=""btn btn-sm btn-outline-danger""><span>{Icons.Delete}</span></a>
                        </div>");
                    }
                }

                sb.AppendLine("</li>");
            }
        }


    }
}
