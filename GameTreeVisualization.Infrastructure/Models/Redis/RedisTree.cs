using Newtonsoft.Json;

namespace GameTreeVisualization.Infrastructure.Models.Redis;

public class RedisTree
{
    [JsonProperty("root")]
    public RedisNode Root { get; set; }
}