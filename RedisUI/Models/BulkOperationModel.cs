using System.Collections.Generic;

namespace RedisUI.Models
{
    public class BulkOperationModel
    {
        public string Operation { get; set; }          // "Delete", "Expire", "Rename"
        public List<string> Keys { get; set; }         // Lista de claves
        public object Args { get; set; }               // TTL para Expire, nuevo prefijo para Rename
    }
}
