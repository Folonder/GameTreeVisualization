using GameTreeVisualization.Models;

namespace GameTreeVisualization.Services.Interfaces;

public interface ITreeProcessingService
{
    Task<TreeNode> ProcessTreeData(string jsonData);
    Task<TreeNode> GetCurrentTree();
    Task<Dictionary<int, int>> CalculateDepthStatistics(TreeNode tree);
}