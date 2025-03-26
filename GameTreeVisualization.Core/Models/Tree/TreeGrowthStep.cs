namespace GameTreeVisualization.Core.Models.Tree;

public class TreeGrowthStep
{
    public int StepNumber { get; set; }
    public int Turn { get; set; }
    public string PatchNumber { get; set; }
    public TreeNode Tree { get; set; }
}