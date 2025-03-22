namespace GameTreeVisualization.Models
{
    public class TreeGrowthStep
    {
        public int StepNumber { get; set; }
        public int Turn { get; set; }
        public int PatchNumber { get; set; }
        public TreeNode Tree { get; set; }
    }
}