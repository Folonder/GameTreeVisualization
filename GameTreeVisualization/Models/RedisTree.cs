// Models/Redis/RedisTreeModel.cs
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GameTreeVisualization.Models.Redis
{
    public class RedisTree
    {
        [JsonProperty("root")]
        public RedisNode Root { get; set; }
    }

    public class RedisNode
    {
        [JsonProperty("children")]
        public List<RedisNode> Children { get; set; } = new List<RedisNode>();
        
        [JsonProperty("state")]
        public List<string> State { get; set; }
        
        [JsonProperty("statistics")]
        public RedisStatistics Statistics { get; set; }
        
        [JsonProperty("isPlayout")]
        public bool IsPlayout { get; set; }
        
        [JsonProperty("precedingJointMove")]
        public RedisPrecedingJointMove PrecedingJointMove { get; set; }
    }

    public class RedisPrecedingJointMove
    {
        [JsonProperty("roles")]
        public List<RedisRole> Roles { get; set; }
        
        [JsonProperty("actionsMap")]
        public Dictionary<string, RedisAction> ActionsMap { get; set; }
    }

    public class RedisAction
    {
        [JsonProperty("contents")]
        public RedisContents Contents { get; set; }
    }

    public class RedisContents
    {
        [JsonProperty("value")]
        public string Value { get; set; }
        
        [JsonProperty("name")]
        public RedisName Name { get; set; }
        
        [JsonProperty("body")]
        public List<RedisItem> Body { get; set; }
    }

    public class RedisItem
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class RedisStatistics
    {
        [JsonProperty("numVisits")]
        public int NumVisits { get; set; }
        
        [JsonProperty("statisticsForActions")]
        public RedisActionsStatistics StatisticsForActions { get; set; }
    }

    public class RedisActionsStatistics
    {
        [JsonProperty("map")]
        public Dictionary<string, Dictionary<string, RedisActionStat>> Map { get; set; }
        
        [JsonProperty("roles")]
        public List<RedisRole> Roles { get; set; }
    }

    public class RedisRole
    {
        [JsonProperty("name")]
        public RedisName Name { get; set; }
    }

    public class RedisName
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class RedisActionStat
    {
        [JsonProperty("actionScore")]
        public double ActionScore { get; set; }
        
        [JsonProperty("actionNumUsed")]
        public int ActionNumUsed { get; set; }
    }
}