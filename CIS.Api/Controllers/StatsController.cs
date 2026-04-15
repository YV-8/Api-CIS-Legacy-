using CIS.BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;

namespace CIS.Api.Controllers;

[ApiController]
[Route("api/v1/stats/topics")]
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
}
