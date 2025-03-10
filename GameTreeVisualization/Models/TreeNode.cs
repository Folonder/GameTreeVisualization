using System.Text.Json.Serialization;

public class TreeNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string State { get; set; }
    public NodeStatistics Statistics { get; set; }
    
    [JsonPropertyName("children")]  // Явно указываем имя свойства в JSON
    public List<TreeNode> Children { get; set; } = new();
    
    public int Depth { get; set; }
}