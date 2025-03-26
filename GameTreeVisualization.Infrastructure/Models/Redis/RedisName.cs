
using Newtonsoft.Json;

namespace GameTreeVisualization.Infrastructure.Models.Redis;

public class RedisName
{
    [JsonProperty("value")]
    public string Value { get; set; }
}