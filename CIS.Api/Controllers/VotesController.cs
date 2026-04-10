using System.Security.Claims;
using CIS.BusinessLogic.Exceptions;
using CIS.BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CIS.Api.Controllers;

[ApiController]
[Route("api/v1/ideas/{ideaId}/votes")]
public class VotesController : ControllerBase
{
    private readonly IVoteService _voteService;

    public VotesController(IVoteService voteService)
    {
        _voteService = voteService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddVote(string ideaId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        try
        {
            var result = await _voteService.AddVoteAsync(ideaId, userId);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete]
    [Authorize]
    public async Task<IActionResult> RemoveVote(string ideaId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        try
        {
            await _voteService.RemoveVoteAsync(ideaId, userId);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
