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