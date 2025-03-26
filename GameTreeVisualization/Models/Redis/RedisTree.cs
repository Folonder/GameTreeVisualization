// Models/Redis/RedisTreeModel.cs

using Newtonsoft.Json;

namespace GameTreeVisualization.Models.Redis
{
    public class RedisTree
    {
        [JsonProperty("root")]
        public RedisNode Root { get; set; }
    }
}