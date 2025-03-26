using Newtonsoft.Json;

namespace GameTreeVisualization.Models;

public class RoleStatistics
{
    [JsonProperty(PropertyName = "role")]
    public string Role { get; set; }
    
    [JsonProperty(PropertyName = "actions")]
    public List<ActionStatistics> Actions { get; set; } = [];
}