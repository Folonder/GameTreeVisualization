using Newtonsoft.Json;

namespace GameTreeVisualization.Infrastructure.Models.Redis;

public class RedisItem
{
    [JsonProperty("value")]
    public string Value { get; set; }
}