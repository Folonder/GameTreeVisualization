using GameTreeVisualization.Models.Tree;

namespace GameTreeVisualization.Services.Interfaces;

public interface ITreeProcessingService
{
    Task<TreeNode> ProcessTreeData(string jsonData);
    Task<TreeNode> GetCurrentTree();
}