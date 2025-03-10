public class TreeNodeDto
{
    public string Id { get; set; }
    public string State { get; set; }
    public NodeStatistics Statistics { get; set; }
    public List<TreeNodeDto> Children { get; set; }
    public int Depth { get; set; }
    public int TotalVisits => Statistics?.NumVisits ?? 0;
}