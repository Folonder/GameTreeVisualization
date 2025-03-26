using Newtonsoft.Json;

namespace GameTreeVisualization.Infrastructure.Models.Redis;

public class RedisRole
{
    [JsonProperty("name")]
    public RedisName Name { get; set; }
}