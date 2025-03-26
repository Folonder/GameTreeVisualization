using GameTreeVisualization.Models.Tree;

namespace GameTreeVisualization.Services.Interfaces;

public interface IGameSessionService
{
    bool SessionExists(string sessionId);
    
    List<int> GetAvailableTurns(string sessionId);
    Task<List<TreeGrowthStep>> GetTurnGrowth(string sessionId, int turnNumber);
}