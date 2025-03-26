using Newtonsoft.Json;

namespace GameTreeVisualization.Infrastructure.Models.Redis;

public class RedisPrecedingJointMove
{
    [JsonProperty("roles")]
    public List<RedisRole> Roles { get; set; }
        
    [JsonProperty("actionsMap")]
    public Dictionary<string, RedisAction> ActionsMap { get; set; }
}