using CIS.BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;
using CIS.BusinessLogic.Exceptions;

namespace CIS.Api.Controllers;
[ApiController]
[Route("api/v1/stats")]
public class StatsController : ControllerBase
{
    private readonly IStatsService _statsService;

    public StatsController(IStatsService statsService)
    {
        _statsService = statsService;
    }

    [HttpGet("top")]
    public async Task<IActionResult> GetTopTopics([FromQuery] int? limit = null)
    {
        if (limit.HasValue && limit <= 0)
        {
            return BadRequest(new { message = "Limit must be greater than 0." });
        }
        var result = await _statsService.GetTopTopicsByActivityAsync(limit);
        return Ok(result);

    }

    [HttpGet("ideas/top")]
    public async Task<IActionResult> GetTopIdeas([FromQuery] string? topicId, [FromQuery] int? limit = null)
    {
    if (limit.HasValue && limit <= 0)
        {
            return BadRequest(new { message = "Limit must be greater than 0." });
        }
        var reslut = await _statsService.GetTopIdeasAsync(topicId, limit);
        return Ok(result);
    }

    
    [HttpGet("topics/{topicId}/ideas/top")]
    public async Task<IActionResult> GetTopIdeasByTopic([FromRoute] string topicId, [FromQuery] int? limit = null)
    {
        if (limit.HasValue && limit <= 0)
        {
            return BadRequest(new { message = "Limit must be greater than 0." });
        }
        try 
        {
            var result = await _statsService.GetTopIdeasAsync(topicId, limit: 200);
            return Ok(result); 
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("users/top")]
    public async Task<IActionResult> GetTopUsers([FromQuery] int? limit = null)
    {
        if (limit.HasValue && limit <= 0)
        {
            return BadRequest(new { message = "Limit must be greater than 0." });
        }
        var topUsers = await _statsService.GetTopUsersAsync(limit);
        return Ok(topUsers);
    }
}
