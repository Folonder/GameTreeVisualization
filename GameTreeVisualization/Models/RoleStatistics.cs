using GameTreeVisualization.Models;

public class RoleStatistics
{
    public string Role { get; set; }
    public List<ActionStatistics> Actions { get; set; } = new();
}