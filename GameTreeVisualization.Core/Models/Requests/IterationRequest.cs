namespace GameTreeVisualization.Core.Models.Requests;

public class IterationRequest
{
    public required string SessionId { get; set; }
    public int TurnNumber { get; set; }
    public int IterationNumber { get; set; }
}