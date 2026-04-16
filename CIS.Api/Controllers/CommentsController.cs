using CIS.Api.Extensions;
using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CIS.Api.Controllers;

[ApiController]
[Route("api/v1/topics/{topicId}/ideas/{ideaId}/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateComment(
        string topicId,
        string ideaId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var comment = await _commentService.CreateCommentAsync(topicId, ideaId, request, userId, cancellationToken);
        return Created($"/api/v1/topics/{topicId}/ideas/{ideaId}/comments/{comment.Id}", comment);
    }

    [HttpGet]
    public async Task<IActionResult> GetComments(
        string topicId,
        string ideaId,
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string[]? sort = null,
        CancellationToken cancellationToken = default)
    {
        const int maxSize = 50;

        if (page < 0)
            return BadRequest(new { message = "Page must be greater than or equal to 0." });

        if (size <= 0 || size > maxSize)
            return BadRequest(new { message = $"Size must be between 1 and {maxSize}." });

        var result = await _commentService.GetCommentsAsync(topicId, ideaId, page, size, sort, cancellationToken);

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
            Response.Headers.Append("Link", string.Join(", ", links));

        return Ok(result);
    }
}
