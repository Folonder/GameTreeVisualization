using GameTreeVisualization.Core.Interfaces;
using GameTreeVisualization.Core.Models.Requests;
using GameTreeVisualization.Core.Models.Tree;
using Microsoft.AspNetCore.Mvc;

namespace GameTreeVisualization.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class GameSessionController : ControllerBase
{
    private readonly IGameDataService _gameDataService;
    private readonly ILogger<GameSessionController> _logger;

    public GameSessionController(
        IGameDataService gameDataService,
        ILogger<GameSessionController> logger)
    {
        _gameDataService = gameDataService;
        _logger = logger;
    }

    [HttpPost("exists")]
    public ActionResult<bool> SessionExists([FromBody] SessionRequest request)
    {
        try
        {
            var exists = _gameDataService.SessionExists(request.SessionId);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking session {request.SessionId}");
            return StatusCode(500, "Error checking session");
        }
    }

    [HttpPost("turns")]
    public ActionResult<List<int>> GetTurns([FromBody] SessionRequest request)
    {
        try
        {
            var turns = _gameDataService.GetAvailableTurnsForSession(request.SessionId);
            return Ok(turns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving turns for session {request.SessionId}");
            return StatusCode(500, "Error retrieving turns");
        }
    }

    [HttpPost("turn/growth")]
    public async Task<ActionResult<List<TreeGrowthStep>>> GetTurnGrowth([FromBody] TurnRequest request)
    {
        try
        {
            var growth = await _gameDataService.GetTreeGrowthSteps(request.SessionId, request.TurnNumber);
            return Ok(growth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Error retrieving turn growth for session {request.SessionId}, turn {request.TurnNumber}");
            return StatusCode(500, "Error retrieving turn growth");
        }
    }

    [HttpPost("turn/tree")]
    public async Task<ActionResult<TreeNode>> GetTreeForTurn([FromBody] TurnRequest request)
    {
        try
        {
            var tree = await _gameDataService.GetTreeForSession(request.SessionId, request.TurnNumber);
            return Ok(tree);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, $"No tree found for session {request.SessionId}, turn {request.TurnNumber}");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                $"Error retrieving tree for session {request.SessionId}, turn {request.TurnNumber}");
            return StatusCode(500, "Error retrieving tree");
        }
    }
    
    [HttpPost("turn/iteration")]
    public async Task<ActionResult<IterationDetails>> GetIterationDetails([FromBody] IterationRequest request)
    {
        try
        {
            var details = await _gameDataService.GetIterationDetails(
                request.SessionId, 
                request.TurnNumber, 
                request.IterationNumber);
            
            return Ok(details);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, $"No iteration details found for session {request.SessionId}, turn {request.TurnNumber}, iteration {request.IterationNumber}");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving iteration details for session {request.SessionId}, turn {request.TurnNumber}, iteration {request.IterationNumber}");
            return StatusCode(500, "Error retrieving iteration details");
        }
    }
}