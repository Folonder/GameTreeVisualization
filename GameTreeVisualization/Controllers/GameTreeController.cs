using System.Text.Json;
using GameTreeVisualization.Models;
using GameTreeVisualization.Models.Tree;
using GameTreeVisualization.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GameTreeVisualization.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameTreeController : ControllerBase
{
    private readonly ITreeProcessingService _treeService;
    private readonly ILogger<GameTreeController> _logger;

    public GameTreeController(
        ITreeProcessingService treeService,
        ILogger<GameTreeController> logger)
    {
        _treeService = treeService;
        _logger = logger;
    }

    [HttpPost("process")]
    public async Task<ActionResult<TreeNode>> ProcessTree([FromBody] object jsonData)
    {
        try
        {
            var jsonString = JsonSerializer.Serialize(jsonData);
            var processedTree = await _treeService.ProcessTreeData(jsonString);
            return Ok(processedTree);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing tree");
            return StatusCode(500, "Error processing tree data");
        }
    }

    [HttpGet("current")]
    public async Task<ActionResult<TreeNode>> GetCurrentTree()
    {
        try
        {
            var tree = await _treeService.GetCurrentTree();
            return Ok(tree);
        }
        catch (InvalidOperationException)
        {
            return NotFound("No tree data available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current tree");
            return StatusCode(500, "Error retrieving tree data");
        }
    }
}