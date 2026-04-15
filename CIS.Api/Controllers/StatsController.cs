using CIS.BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;
using CIS.BusinessLogic.Exceptions;

[ApiController]
[Route("api/v1/stats")]
public class StatsController : ControllerBase
{
    private readonly IIdeaService _ideaService;
    public StatsController(IIdeaService ideaService)
    {
        _ideaService = ideaService;
    }

    [HttpGet("ideas/top")]
    public async Task<IActionResult> GetTopIdeas([FromQuery] string? topicId, [FromQuery] int limit = 200)
    {
        if (limit <= 0)
        {
            return BadRequest(new { message = "Limit must be greater than 0." });
        }
        var topIdeas = await _ideaService.GetTopIdeasAsync(topicId, limit);
        return Ok(topIdeas);
    }
    [HttpGet("topics/{topicId}/ideas/top")]
    public async Task<IActionResult> GetTopIdeasByTopic([FromRoute] string topicId, [FromQuery] int limit = 200)
    {
        if (limit <= 0)
        {
            return BadRequest(new { message = "Limit must be greater than 0." });
        }
        try 
        {
            var result = await _ideaService.GetTopIdeasAsync(topicId, limit: 200);
            return Ok(result); // Si no hay ideas, retorna [] automáticamente por el ToListAsync
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("users/top")]
    public async Task<IActionResult> GetTopUsers([FromQuery] int limit = 200)
    {
        if (limit <= 0)
        {
            return BadRequest(new { message = "Limit must be greater than 0." });
        }
        var topUsers = await _ideaService.GetTopUsersAsync(limit);
        return Ok(topUsers);
    }
}  