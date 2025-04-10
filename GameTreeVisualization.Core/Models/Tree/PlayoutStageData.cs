
using System.Text.Json.Serialization;

namespace GameTreeVisualization.Core.Models.Tree;

public class PlayoutStageData
{
    [JsonPropertyName("startNode")]
    public TreeNode StartNode { get; set; }
    
    [JsonPropertyName("depth")]
    public int Depth { get; set; }
    
    [JsonPropertyName("results")]
    public Dictionary<string, double> Results { get; set; } = new Dictionary<string, double>();
}