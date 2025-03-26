using Newtonsoft.Json;

namespace GameTreeVisualization.Models.Redis;

public class RedisAction
{
    [JsonProperty("contents")]
    public RedisContents Contents { get; set; }
}