using System.Text.Json.Serialization;
using GameTreeVisualization.Converters;

namespace GameTreeVisualization.Models;

public class ActionStatistics
{
    public string Action { get; set; }
    
    [JsonConverter(typeof(DoubleConverter))]
    public double AverageActionScore { get; set; }
    
    public int ActionNumUsed { get; set; }
}