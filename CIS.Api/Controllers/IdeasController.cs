using System.Security.Claims;
using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Exceptions;
using CIS.BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CIS.Api.Controllers;

[ApiController]
[Route("api/v1/topics/{topicId}/ideas")]
public class IdeasController : ControllerBase
{
    private readonly IIdeaService _ideaService;

    public IdeasController(IIdeaService ideaService)
    {
        _ideaService = ideaService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateIdea(string topicId, [FromBody] CreateIdeaRequest request)
    {
        try
        {
            var authorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(authorId))
                return Unauthorized();

            var createdIdea = await _ideaService.CreateIdeaAsync(topicId, request, authorId);

            return Created($"/api/v1/topics/{topicId}/ideas/{createdIdea.Id}", createdIdea);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetIdeas(
        string topicId,
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string? authorId = null,
        [FromQuery] string? sort = "voteCount,desc")
    {
        try
        {
            var result = await _ideaService.GetIdeasAsync(topicId, page, size, authorId, sort);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{ideaId}")]
[Authorize]
public async Task<IActionResult> UpdateIdea(string topicId, string ideaId, [FromBody] UpdateIdeaRequest request)
{
    try
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        var updatedIdea = await _ideaService.UpdateIdeaAsync(topicId, ideaId, request, currentUserId);
        return Ok(new
        {
            currentUserId,
            data = updatedIdea
        });
    }
    catch (NotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
    catch (ForbiddenException ex)
    {
        return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
    }
}
    

    [HttpDelete("{ideaId}")]
[Authorize]
public async Task<IActionResult> DeleteIdea(string topicId, string ideaId)
{
    try
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        await _ideaService.DeleteIdeaAsync(topicId, ideaId, currentUserId);

        return Ok(new
        {
            message = "Idea deleted",
            currentUserId
        });
    }
    catch (NotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
    catch (ForbiddenException ex)
    {
        return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
    }
}
}