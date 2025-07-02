using System.Text.Json;

namespace RedisUI.Models
{
    public class KeyInputModel
    {
        public string Name { get; set; }
        public string KeyType { get; set; }

        public JsonElement? Value { get; set; } // recibimos valor crudo

        public int? TTL { get; set; } // en segundos
    }

}
