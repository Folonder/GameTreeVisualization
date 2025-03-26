using Newtonsoft.Json;

namespace GameTreeVisualization.Infrastructure.Models.Redis;

public class RedisAction
{
    [JsonProperty("contents")]
    public RedisContents Contents { get; set; }
}