using Newtonsoft.Json;

namespace GameTreeVisualization.Models.Redis;

public class RedisActionStat
{
    [JsonProperty("actionScore")]
    public double ActionScore { get; set; }
        
    [JsonProperty("actionNumUsed")]
    public int ActionNumUsed { get; set; }
}