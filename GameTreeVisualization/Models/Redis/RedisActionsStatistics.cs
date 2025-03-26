using Newtonsoft.Json;

namespace GameTreeVisualization.Models.Redis;

public class RedisActionsStatistics
{
    [JsonProperty("map")]
    public Dictionary<string, Dictionary<string, RedisActionStat>> Map { get; set; }
        
    [JsonProperty("roles")]
    public List<RedisRole> Roles { get; set; }
}