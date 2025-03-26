namespace GameTreeVisualization.Models.Requests
{
    public class TurnRequest
    {
        public required string SessionId { get; set; }
        public int TurnNumber { get; set; }
    }
}