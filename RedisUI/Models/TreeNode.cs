using System.Collections.Generic;

namespace RedisUI.Models
{
    public class TreeNode
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, TreeNode> Children { get; set; } = new();
        public List<KeyModel> Keys { get; set; } = new(); // Solo en hojas
    }
}
