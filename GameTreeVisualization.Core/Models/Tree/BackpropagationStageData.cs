using System.Text.Json.Serialization;

namespace GameTreeVisualization.Core.Models.Tree;

public class BackpropagationStageData
{
    [JsonPropertyName("path")]
    public List<TreeNode> Path { get; set; } = new List<TreeNode>();
    
    [JsonPropertyName("results")]
    public Dictionary<string, double> Results { get; set; } = new Dictionary<string, double>();
}