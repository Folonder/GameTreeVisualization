namespace GameTreeVisualization.Models
{
    public class TurnRequest
    {
        public required string SessionId { get; set; }
        public int TurnNumber { get; set; }
    }
}