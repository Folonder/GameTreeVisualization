using GameTreeVisualization.Models;
using GameTreeVisualization.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GameTreeVisualization.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameSessionController : ControllerBase
    {
        private readonly IGameSessionService _sessionService;
        private readonly ILogger<GameSessionController> _logger;

        public GameSessionController(
            IGameSessionService sessionService,
            ILogger<GameSessionController> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        [HttpPost("exists")]
        public ActionResult<bool> SessionExists([FromBody] SessionRequest request)
        {
            try
            {
                var exists = _sessionService.SessionExists(request.SessionId);
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
                var turns = _sessionService.GetAvailableTurns(request.SessionId);
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
                var growth = await _sessionService.GetTurnGrowth(request.SessionId, request.TurnNumber);
                return Ok(growth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"Error retrieving turn growth for session {request.SessionId}, turn {request.TurnNumber}");
                return StatusCode(500, "Error retrieving turn growth");
            }
        }
    }
}