using System.Text.Json.Serialization;

namespace GameTreeVisualization.Core.Models.Tree;

public class NodeStatistics
{
    [JsonPropertyName("numVisits")]
    public int NumVisits { get; set; }
    
    [JsonPropertyName("relativeVisits")]
    public double RelativeVisits { get; set; }
    
    [JsonPropertyName("statisticsForActions")]
    public List<RoleStatistics> StatisticsForActions { get; set; } = [];
}