using CIS.BusinessLogic.Services;
using CIS.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;

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
    public async Task<IActionResult> AddVote(string ideaId, CancellationToken cancellationToken)
    {
        var userId = User.GetSubjectId();

        var result = await _voteService.AddVoteAsync(ideaId, userId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete]
    [Authorize]
    public async Task<IActionResult> RemoveVote(string ideaId, CancellationToken cancellationToken)
    {
        var userId = User.GetSubjectId();

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        await _voteService.RemoveVoteAsync(ideaId, userId, cancellationToken);
        return NoContent();
    }
}
