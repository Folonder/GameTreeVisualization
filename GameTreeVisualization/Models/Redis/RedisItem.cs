using Newtonsoft.Json;

namespace GameTreeVisualization.Models.Redis;

public class RedisItem
{
    [JsonProperty("value")]
    public string Value { get; set; }
}