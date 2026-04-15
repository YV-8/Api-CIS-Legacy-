using CIS.BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;
using CIS.BusinessLogic.Exceptions;

[ApiController]
[Route("api/v1/stats")]
public class StatsController : ControllerBase
{
    private readonly IStatsService _statsService;
    public StatsController(IStatsService statsService)
    {
        _statsService = statsService;
    }

    [HttpGet("ideas/top")]
    public async Task<IActionResult> GetTopIdeas([FromQuery] string? topicId, [FromQuery] int? limit = null)
    {
        if (limit.HasValue && limit <= 0)
        {
            return BadRequest(new { message = "Limit must be greater than 0." });
        }
        var topIdeas = await _statsService.GetTopIdeasAsync(topicId, limit);
        return Ok(topIdeas);
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