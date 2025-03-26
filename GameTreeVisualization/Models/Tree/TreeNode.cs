using Newtonsoft.Json;

namespace GameTreeVisualization.Models.Tree;

public class TreeNode
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [JsonProperty(PropertyName = "state")]
    public string State { get; set; }
    
    [JsonProperty(PropertyName = "statistics")]
    public NodeStatistics Statistics { get; set; }
    
    [JsonProperty(PropertyName = "children")]
    public List<TreeNode> Children { get; set; } = [];
    
    public int Depth { get; set; }
}