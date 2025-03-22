using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameTreeVisualization.Models;
using GameTreeVisualization.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
        public async Task<ActionResult<bool>> SessionExists([FromBody] SessionRequest request)
        {
            try
            {
                var exists = await _sessionService.SessionExists(request.SessionId);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking session {request.SessionId}");
                return StatusCode(500, "Error checking session");
            }
        }

        [HttpPost("initial")]
        public async Task<ActionResult<TreeNode>> GetInitialTree([FromBody] SessionRequest request)
        {
            try
            {
                var tree = await _sessionService.GetInitialTree(request.SessionId);
                return Ok(tree);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving initial tree for session {request.SessionId}");
                return StatusCode(500, "Error retrieving initial tree");
            }
        }

        [HttpPost("patches")]
        public async Task<ActionResult<List<TreePatch>>> GetTreePatches([FromBody] SessionRequest request)
        {
            try
            {
                var patches = await _sessionService.GetPatches(request.SessionId);
                return Ok(patches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving patches for session {request.SessionId}");
                return StatusCode(500, "Error retrieving tree patches");
            }
        }

        [HttpPost("growth")]
        public async Task<ActionResult<List<TreeGrowthStep>>> GetTreeGrowth([FromBody] SessionRequest request)
        {
            try
            {
                _logger.LogInformation($"Calculating tree growth for session {request.SessionId} with JSON Patch format");
                var growth = await _sessionService.CalculateTreeGrowth(request.SessionId);
                return Ok(growth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating tree growth for session {request.SessionId}");
                return StatusCode(500, "Error calculating tree growth");
            }
        }
    }
}