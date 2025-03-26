using Newtonsoft.Json;

namespace GameTreeVisualization.Infrastructure.Models.Redis;

public class RedisActionStat
{
    [JsonProperty("actionScore")]
    public double ActionScore { get; set; }
        
    [JsonProperty("actionNumUsed")]
    public int ActionNumUsed { get; set; }
}