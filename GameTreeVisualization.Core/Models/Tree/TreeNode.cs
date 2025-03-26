using System.Text.Json.Serialization;

namespace GameTreeVisualization.Core.Models.Tree;

public class TreeNode
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("state")]
    public string State { get; set; }
    
    [JsonPropertyName("statistics")]
    public NodeStatistics Statistics { get; set; }
    
    [JsonPropertyName("children")]
    public List<TreeNode> Children { get; set; } = [];
    
    public int Depth { get; set; }
}