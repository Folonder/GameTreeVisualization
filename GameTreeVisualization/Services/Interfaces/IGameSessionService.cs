using GameTreeVisualization.Models;

namespace GameTreeVisualization.Services.Interfaces;

public interface IGameSessionService
{
    Task<bool> SessionExists(string sessionId);
    Task<TreeNode> GetInitialTree(string sessionId);
    Task<List<TreePatch>> GetPatches(string sessionId);
    Task<List<TreeGrowthStep>> CalculateTreeGrowth(string sessionId);
}