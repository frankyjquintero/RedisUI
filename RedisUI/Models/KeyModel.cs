using RedisUI.Helpers;
using StackExchange.Redis;

namespace RedisUI.Models
{
    public class KeyModel
    {
        public string Name { get; set; }

        public RedisType KeyType { get; set; }

        public RedisKeyDetails Detail { get; set; }

    }
}
