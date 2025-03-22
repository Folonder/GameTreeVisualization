namespace GameTreeVisualization.Models
{
    public class TreePatch
    {
        public int Turn { get; set; }
        public int PatchNumber { get; set; }
        public List<PatchOperation> Operations { get; set; } = new();
    }
}