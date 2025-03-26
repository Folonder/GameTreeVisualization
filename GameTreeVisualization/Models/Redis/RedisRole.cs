using Newtonsoft.Json;

namespace GameTreeVisualization.Models.Redis;

public class RedisRole
{
    [JsonProperty("name")]
    public RedisName Name { get; set; }
}