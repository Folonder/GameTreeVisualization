using Newtonsoft.Json;

namespace GameTreeVisualization.Infrastructure.Models.Redis;

public class RedisStatistics
{
    [JsonProperty("numVisits")]
    public int NumVisits { get; set; }
        
    [JsonProperty("statisticsForActions")]
    public RedisActionsStatistics StatisticsForActions { get; set; }
}