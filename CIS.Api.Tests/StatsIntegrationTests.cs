using CIS.BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading;

namespace CIS.Api.Controllers;

[ApiController]
[Route("api/v1/stats")]
public class StatsIntegrationTest : ControllerBase
{
    private readonly IStatsService _statsService;

    public StatsIntegrationTest(IStatsService statsService)
    {
        _statsService = statsService;
    }

    [HttpGet("top")]
    public async Task<IActionResult> GetTopTopics([FromQuery] int? limit = null, CancellationToken cancellationToken = default)
    {
        if (limit.HasValue && limit <= 0)
        {
            return BadRequest(new { message = "Limit must be greater than 0." });
        }

        var result = await _statsService.GetTopTopicsByActivityAsync(limit, cancellationToken);
        return Ok(result);
    }

    [HttpGet("ideas/top")]
    public async Task<IActionResult> GetTopIdeas([FromQuery] string? topicId, [FromQuery] int? limit = null, CancellationToken cancellationToken = default)
    {
        if (limit.HasValue && limit <= 0)
        {
            return BadRequest(new { message = "Limit must be greater than 0." });
        }

        var result = await _statsService.GetTopIdeasAsync(topicId, limit, cancellationToken);
        return Ok(result);
    }

    [HttpGet("topics/{topicId}/ideas/top")]
    public async Task<IActionResult> GetTopIdeasByTopic([FromRoute] string topicId, [FromQuery] int? limit = null, CancellationToken cancellationToken = default)
    {
        if (limit.HasValue && limit <= 0)
        {
            return BadRequest(new { message = "Limit must be greater than 0." });
        }

        var result = await _statsService.GetTopIdeasAsync(topicId, limit, cancellationToken);
        return Ok(result);
    }

    [HttpGet("users/top")]
    public async Task<IActionResult> GetTopUsers([FromQuery] int? limit = null, CancellationToken cancellationToken = default)
    {
        if (limit.HasValue && limit <= 0)
        {
            return BadRequest(new { message = "Limit must be greater than 0." });
        }

        var result = await _statsService.GetTopUsersAsync(limit, cancellationToken);
        return Ok(result);
    }
}