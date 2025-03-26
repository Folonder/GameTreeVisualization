using Newtonsoft.Json;

namespace GameTreeVisualization.Models.Redis;

public class RedisContents
{
    [JsonProperty("value")]
    public string Value { get; set; }
        
    [JsonProperty("name")]
    public RedisName Name { get; set; }
        
    [JsonProperty("body")]
    public List<RedisItem> Body { get; set; }
}