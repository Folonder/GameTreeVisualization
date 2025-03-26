using System.Text.Json.Serialization;

namespace GameTreeVisualization.Core.Models.Tree;

public class RoleStatistics
{
    [JsonPropertyName("role")]
    public string Role { get; set; }
    
    [JsonPropertyName("actions")]
    public List<ActionStatistics> Actions { get; set; } = [];
}