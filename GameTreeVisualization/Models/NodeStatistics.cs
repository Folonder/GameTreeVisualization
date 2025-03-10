public class NodeStatistics
{
    public int NumVisits { get; set; }
    public double RelativeVisits { get; set; }
    public List<RoleStatistics> StatisticsForActions { get; set; } = new();
}