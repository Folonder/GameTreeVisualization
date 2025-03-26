using GameTreeVisualization.Core.Models.Tree;

namespace GameTreeVisualization.Core.Interfaces;

public interface IGameDataService
{
    Task<TreeNode> GetTreeForSession(string sessionId, int turnNumber);
    List<int> GetAvailableTurnsForSession(string sessionId);
    Task<List<TreeGrowthStep>> GetTreeGrowthSteps(string sessionId, int turnNumber);
    bool SessionExists(string sessionId);
}