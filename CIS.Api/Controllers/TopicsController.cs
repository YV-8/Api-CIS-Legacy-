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
    public async Task<IActionResult> GetTopics([FromQuery] TopicFilterRequest filter)
    {
        const int maxSize = 50;
        if (filter.Size > maxSize) filter.Size = maxSize;

        DateTime? fromDate = null;
        DateTime? toDate = null;

        if (!string.IsNullOrEmpty(filter.CreatedFrom))
        {
            if (!DateTime.TryParse(filter.CreatedFrom, out var d))
                return BadRequest(new { message = "Invalid createdFrom date format. Use YYYY-MM-DD." });
            fromDate = d;
        }

        if (!string.IsNullOrEmpty(filter.CreatedTo))
        {
            if (!DateTime.TryParse(filter.CreatedTo, out var dt))
                return BadRequest(new { message = "Invalid createdTo date format. Use YYYY-MM-DD." });
            toDate = dt;
        }

        PaginatedResponse<TopicResponse> result;
        try
        {
            result = await _topicService.GetTopicsAsync(
                filter.Page, 
                filter.Size, 
                filter.AuthorId, 
                fromDate, 
                toDate, 
                filter.Sort);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
           


        DateTime? fromDate = null;
        DateTime? toDate = null;

        if (!string.IsNullOrEmpty(filter.CreatedFrom) && DateTime.TryParse(filter.CreatedFrom, out var f))
        {
            fromDate = f;
        }

        if (!string.IsNullOrEmpty(filter.CreatedTo) && DateTime.TryParse(filter.CreatedTo, out var t))
        {
            toDate = t;
        }

<<<<<<< CIS.Api/Controllers/TopicsController.cs
        PaginatedResponse<TopicResponse> result;
        try
        {
            result = await _topicService.GetTopicsAsync(page, size, authorId, fromDate, toDate, sort);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
=======
        if (!string.IsNullOrEmpty(filter.CreatedFrom))
        {
            if (!DateTime.TryParse(filter.CreatedFrom, out var d))
                return BadRequest(new { message = "Invalid createdFrom date format. Use YYYY-MM-DD." });
            fromDate = d;
        }

        if (!string.IsNullOrEmpty(filter.CreatedTo))
        {
            if (!DateTime.TryParse(filter.CreatedTo, out var dt))
                return BadRequest(new { message = "Invalid createdTo date format. Use YYYY-MM-DD." });
            toDate = dt;
        }

        var result = await _topicService.GetTopicsAsync(
        filter.Page, 
        filter.Size, 
        filter.AuthorId, 
        fromDate, 
        toDate, 
        filter.Sort);
>>>>>>> CIS.Api/Controllers/TopicsController.cs

        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
        var links = new List<string>();

        if (filter.Page > 0)
        {
            links.Add($"<{baseUrl}?page=0&size={filter.Size}>; rel=\"first\"");
            links.Add($"<{baseUrl}?page={filter.Page - 1}&size={filter.Size}>; rel=\"prev\"");
        }

        if (filter.Page < result.TotalPages - 1)
        {
            links.Add($"<{baseUrl}?page={filter.Page + 1}&size={filter.Size}>; rel=\"next\"");
            links.Add($"<{baseUrl}?page={result.TotalPages - 1}&size={filter.Size}>; rel=\"last\"");
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