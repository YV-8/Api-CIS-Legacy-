using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CIS.BusinessLogic.dtos;
using CIS.DataAcces.Models;
using CIS.BusinessLogic.Services;

[ApiController]
[Route("api/v1/[controller]")]
public class TopicsController : ControllerBase
{
    private readonly ITopicService _topicService;

    public TopicsController(ITopicService topicService)
    {
        _topicService = topicService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTopics(
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string? authorId = null,
        [FromQuery] string? createdFrom = null,
        [FromQuery] string? createdTo = null,
        [FromQuery] string[]? sort = null)
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

        DateTime? fromDate = null;
        DateTime? toDate = null;

        if (!string.IsNullOrEmpty(createdFrom) && DateTime.TryParse(createdFrom, out var f))
        {
            fromDate = f;
        }

        if (!string.IsNullOrEmpty(createdTo) && DateTime.TryParse(createdTo, out var t))
        {
            toDate = t;
        }

        PaginatedResponse<TopicResponse> result;
        try
        {
            result = await _topicService.GetTopicsAsync(page, size, authorId, fromDate, toDate, sort);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

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

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateTopicRequest request)
    {
        var authorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value;

        if (authorId == null) return Unauthorized();

        var newTopic = await _topicService.CreateTopicAsync(request, authorId);

        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}/{newTopic.Id}";

        var response = new
        {
            newTopic.Id,
            newTopic.Title,
            newTopic.Description,
            newTopic.AuthorId,
            newTopic.CreatedAt,
            newTopic.Status,
            Links = new object[]
            {
                new { rel = "self", href = baseUrl },
                new { rel = "ideas", href = $"{baseUrl}/ideas" },
                new { rel = "update", href = baseUrl, method = "PUT" },
                new { rel = "delete", href = baseUrl, method = "DELETE" }
            }
        };

        return CreatedAtAction(nameof(Create), new { id = newTopic.Id }, response);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(string id)
    {
        var topic = await _topicService.GetTopicByIdAsync(id);

        if (topic == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/v1/topics/{topic.Id}";

        var response = new
        {
            topic.Id,
            topic.Title,
            topic.Description,
            topic.AuthorId,
            topic.CreatedAt,
            topic.Status,
            Links = new object[]
            {
                new { rel = "self", href = baseUrl },
                new { rel = "ideas", href = $"{baseUrl}/ideas" },
                new { rel = "author", href = $"{Request.Scheme}://{Request.Host}/api/v1/users/{topic.AuthorId}" }
            }
        };

        return Ok(response);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTopicRequest request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;

        if (requesterId == null) return Unauthorized();

        try
        {
            var updated = await _topicService.UpdateTopicAsync(id, request, requesterId);

            var baseUrl = $"{Request.Scheme}://{Request.Host}/api/v1/topics/{updated.Id}";

            var response = new
            {
                updated.Id,
                updated.Title,
                updated.Description,
                updated.AuthorId,
                updated.CreatedAt,
                updated.UpdatedAt,
                updated.Status,
                Links = new object[]
                {
                    new { rel = "self", href = baseUrl },
                    new { rel = "ideas", href = $"{baseUrl}/ideas" },
                    new { rel = "author", href = $"{Request.Scheme}://{Request.Host}/api/v1/users/{updated.AuthorId}" }
                }
            };

            return Ok(response);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(string id)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;

        if (requesterId == null) return Unauthorized();

        try
        {
            await _topicService.DeleteTopicAsync(id, requesterId);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}