using System.Text.Json.Serialization;

namespace GameTreeVisualization.Core.Models.Tree;

public class SelectionStageData
{
    [JsonPropertyName("path")]
    public List<TreeNode> Path { get; set; } = new List<TreeNode>();
    
    [JsonPropertyName("selectedNode")]
    public TreeNode SelectedNode { get; set; }
}