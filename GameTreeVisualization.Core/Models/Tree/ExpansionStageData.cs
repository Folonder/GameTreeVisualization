using System.Text.Json.Serialization;

namespace GameTreeVisualization.Core.Models.Tree;

public class ExpansionStageData
{
    [JsonPropertyName("expandedNode")]
    public TreeNode ExpandedNode { get; set; }
    
    [JsonPropertyName("newNodes")]
    public List<TreeNode> NewNodes { get; set; } = new List<TreeNode>();
    
    [JsonPropertyName("nodeForPlayout")]
    public TreeNode NodeForPlayout { get; set; }
}