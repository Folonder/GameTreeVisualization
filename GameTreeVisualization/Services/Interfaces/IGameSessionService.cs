using GameTreeVisualization.Models;

namespace GameTreeVisualization.Services.Interfaces;

public interface IGameSessionService
{
    Task<bool> SessionExists(string sessionId);
    Task<TreeNode> GetInitialTree(string sessionId);
    Task<List<TreePatch>> GetPatches(string sessionId);
    Task<List<TreeGrowthStep>> CalculateTreeGrowth(string sessionId);
    
    // Новые методы
    Task<List<int>> GetAvailableTurns(string sessionId);
    Task<TreeNode> GetTurnInitialTree(string sessionId, int turnNumber);
    Task<List<TreeGrowthStep>> GetTurnGrowth(string sessionId, int turnNumber);
}