namespace GameTreeVisualization.Services.Interfaces;

public interface ITreeStorageService
{
    Task<TreeNode> GetStoredTree();
    Task StoreTree(TreeNode tree);
    Task<bool> TreeExists();
    Task ClearStorage();
}