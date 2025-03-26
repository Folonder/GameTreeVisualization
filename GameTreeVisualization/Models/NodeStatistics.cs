using Newtonsoft.Json;

public class NodeStatistics
{
    [JsonProperty(PropertyName = "numVisits")]
    public int NumVisits { get; set; }
    
    [JsonProperty(PropertyName = "relativeVisits")]
    public double RelativeVisits { get; set; }
    
    [JsonProperty(PropertyName = "statisticsForActions")]
    public List<RoleStatistics> StatisticsForActions { get; set; } = new List<RoleStatistics>();
}