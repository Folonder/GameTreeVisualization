using System.Text.Json.Serialization;

namespace GameTreeVisualization.Core.Models.Tree;

public class IterationDetails
{
    [JsonPropertyName("iterationNumber")]
    public int IterationNumber { get; set; }
    
    [JsonPropertyName("turnNumber")]
    public int TurnNumber { get; set; }
    
    [JsonPropertyName("selection")]
    public SelectionStageData Selection { get; set; }
    
    [JsonPropertyName("expansion")]
    public ExpansionStageData Expansion { get; set; }
    
    [JsonPropertyName("playout")]
    public PlayoutStageData Playout { get; set; }
    
    [JsonPropertyName("backpropagation")]
    public BackpropagationStageData Backpropagation { get; set; }
}
