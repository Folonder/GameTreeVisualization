using GameTreeVisualization.Models;
using Newtonsoft.Json;

public class RoleStatistics
{
    [JsonProperty(PropertyName = "role")]
    public string Role { get; set; }
    
    [JsonProperty(PropertyName = "actions")]
    public List<ActionStatistics> Actions { get; set; } = new List<ActionStatistics>();
}