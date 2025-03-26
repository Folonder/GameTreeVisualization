using Newtonsoft.Json;

namespace GameTreeVisualization.Models;

public class ActionStatistics
{
    [JsonProperty(PropertyName = "action")]
    public string Action { get; set; }
    
    [JsonProperty(PropertyName = "averageActionScore")]
    public double AverageActionScore { get; set; }
    
    [JsonProperty(PropertyName = "actionNumUsed")]
    public int ActionNumUsed { get; set; }
}