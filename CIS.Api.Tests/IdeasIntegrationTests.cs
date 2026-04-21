using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Services;
using CIS.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;

namespace CIS.Api.Tests;

[ApiController]
[Route("api/v1/topics/{topicId}/ideas")]
public class IdeasIntegrationTest : ControllerBase
{
    private readonly IIdeaService _ideaService;
 
    public IdeasIntegrationTest(IIdeaService ideaService)
    {
        _ideaService = ideaService;
    }
 
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateIdea(string topicId, [FromBody] CreateIdeaRequest request, CancellationToken cancellationToken)
    {
        var authorId = User.GetSubjectId();
 
        if (string.IsNullOrWhiteSpace(authorId))
            return Unauthorized();
 
        var createdIdea = await _ideaService.CreateIdeaAsync(topicId, request, authorId, cancellationToken);
 
        return Created($"/api/v1/topics/{topicId}/ideas/{createdIdea.Id}", createdIdea);
    }
 
    [HttpGet]
    public async Task<IActionResult> GetIdeas(
        string topicId,
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string? authorId = null,
        [FromQuery] string[]? sort = null,
        CancellationToken cancellationToken = default)
    {
        const int maxSize = 50;
 
        if (page < 0)
        {
            return BadRequest(new { message = "Page must be greater than or equal to 0." });
        }
 
        if (size <= 0 || size > maxSize)
        {
            return BadRequest(new { message = $"Size must be between 1 and {maxSize}." });
        }
 
        var result = await _ideaService.GetIdeasAsync(topicId, page, size, authorId, sort, cancellationToken);
 
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
        var links = new List<string>();
 
        if (page > 0)
        {
            links.Add($"<{baseUrl}?page=0&size={size}>; rel=\"first\"");
            links.Add($"<{baseUrl}?page={page - 1}&size={size}>; rel=\"prev\"");
        }
 
        if (page < result.TotalPages - 1)
        {
            links.Add($"<{baseUrl}?page={page + 1}&size={size}>; rel=\"next\"");
            links.Add($"<{baseUrl}?page={result.TotalPages - 1}&size={size}>; rel=\"last\"");
        }
 
        if (links.Any())
        {
            Response.Headers.Append("Link", string.Join(", ", links));
        }
 
        return Ok(result);
    }
 
    [HttpPut("{ideaId}")]
    [Authorize]
    public async Task<IActionResult> UpdateIdea(string topicId, string ideaId, [FromBody] UpdateIdeaRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetSubjectId();
        var currentUserRole = User.GetRole();
 
        if (string.IsNullOrWhiteSpace(currentUserId) || string.IsNullOrWhiteSpace(currentUserRole))
            return Unauthorized();
 
        var updatedIdea = await _ideaService.UpdateIdeaAsync(topicId, ideaId, request, currentUserId, currentUserRole, cancellationToken);
        return Ok(new
        {
            currentUserId,
            data = updatedIdea
        });
    }
 
    [HttpDelete("{ideaId}")]
    [Authorize]
    public async Task<IActionResult> DeleteIdea(string topicId, string ideaId, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetSubjectId();
        var currentUserRole = User.GetRole();
 
        if (string.IsNullOrWhiteSpace(currentUserId) || string.IsNullOrWhiteSpace(currentUserRole))
            return Unauthorized();
 
        await _ideaService.DeleteIdeaAsync(topicId, ideaId, currentUserId, currentUserRole, cancellationToken);
 
        return Ok(new
        {
            message = "Idea deleted",
            currentUserId
        });
    }
}
