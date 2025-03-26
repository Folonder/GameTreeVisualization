using Newtonsoft.Json;

namespace GameTreeVisualization.Models.Redis;

public class RedisName
{
    [JsonProperty("value")]
    public string Value { get; set; }
}