using Newtonsoft.Json;

namespace GameTreeVisualization.Infrastructure.Models.Redis;

public class RedisNode
{
    [JsonProperty("children")]
    public List<RedisNode> Children { get; set; } = new List<RedisNode>();
        
    [JsonProperty("state")]
    public List<string> State { get; set; }
        
    [JsonProperty("statistics")]
    public RedisStatistics Statistics { get; set; }
        
    [JsonProperty("isPlayout")]
    public bool IsPlayout { get; set; }
        
    [JsonProperty("precedingJointMove")]
    public RedisPrecedingJointMove PrecedingJointMove { get; set; }
}