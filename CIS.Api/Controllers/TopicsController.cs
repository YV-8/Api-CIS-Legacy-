using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CIS.BusinessLogic.dtos;
using CIS.DataAcces.Data;
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
        var newTopic = new Topic
        {
            Title = request.Title,
            Description = request.Description,
            AuthorId = authorId!,
            Status = TopicStatus.draft
        };
        newTopic = await _topicService.CreateTopicAsync(request, authorId!);
        // 3. Crear links HATEOAS
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



}