using Newtonsoft.Json;

public class TreeNode
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [JsonProperty(PropertyName = "state")]
    public string State { get; set; }
    
    [JsonProperty(PropertyName = "statistics")]
    public NodeStatistics Statistics { get; set; }
    
    [JsonProperty(PropertyName = "children")]
    public List<TreeNode> Children { get; set; } = new List<TreeNode>();
    
    public int Depth { get; set; }
}