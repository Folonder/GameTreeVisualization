using GameTreeVisualization.Models.Tree;

namespace GameTreeVisualization.Services.Interfaces;

public interface ITreeStorageService
{
    Task<TreeNode> GetStoredTree();
    Task StoreTree(TreeNode tree);
}