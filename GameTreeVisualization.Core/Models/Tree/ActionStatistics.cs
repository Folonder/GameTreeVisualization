using System.Text.Json.Serialization;

namespace GameTreeVisualization.Core.Models.Tree;

public class ActionStatistics
{
    [JsonPropertyName("action")]
    public string Action { get; set; }
    
    [JsonPropertyName("averageActionScore")]
    public double AverageActionScore { get; set; }
    
    [JsonPropertyName("actionNumUsed")]
    public int ActionNumUsed { get; set; }
}