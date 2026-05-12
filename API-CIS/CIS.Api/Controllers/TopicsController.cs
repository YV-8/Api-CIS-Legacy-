using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Services;
using CIS.Api.Extensions;
using System.Threading;
using Microsoft.AspNetCore.WebUtilities;

namespace CIS.Api.Controllers
{
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
        public async Task<IActionResult> GetTopics([FromQuery] TopicFilterRequest filter, CancellationToken cancellationToken)
        {
            const int maxSize = 50;

            if (filter.Page < 0)
            {
                return BadRequest(new { message = "Page must be greater than or equal to 0." });
            }

            if (filter.Size < 1 || filter.Size > maxSize)
            {
                return BadRequest(new { message = $"Size must be between 1 and {maxSize}." });
            }

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

            var result = await _topicService.GetTopicsAsync(
                filter.Page,
                filter.Size,
                filter.AuthorId,
                fromDate,
                toDate,
                filter.Sort,
                cancellationToken);

            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
            
            string CreateResourceUri(int pageNumber)
            {
                var queryParams = new Dictionary<string, string?>
                {
                    ["page"] = pageNumber.ToString(),
                    ["size"] = filter.Size.ToString()
                };

                if (!string.IsNullOrEmpty(filter.AuthorId)) queryParams["authorId"] = filter.AuthorId;
                if (!string.IsNullOrEmpty(filter.CreatedFrom)) queryParams["createdFrom"] = filter.CreatedFrom;
                if (!string.IsNullOrEmpty(filter.CreatedTo)) queryParams["createdTo"] = filter.CreatedTo;

                var uri = QueryHelpers.AddQueryString(baseUrl, queryParams);

                if (filter.Sort != null)
                {
                    foreach (var sort in filter.Sort)
                    {
                        uri = QueryHelpers.AddQueryString(uri, "sort", sort);
                    }
                }

                return uri;
            }

            var links = new Dictionary<string, object>
            {
                ["self"] = new { href = CreateResourceUri(filter.Page) },
                ["first"] = new { href = CreateResourceUri(0) },
                ["last"] = new { href = CreateResourceUri(Math.Max(0, result.TotalPages - 1)) }
            };

            if (filter.Page > 0)
            {
                links["prev"] = new { href = CreateResourceUri(filter.Page - 1) };
            }

            if (filter.Page < result.TotalPages - 1)
            {
                links["next"] = new { href = CreateResourceUri(filter.Page + 1) };
            }

            result.Links = links;

            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateTopicRequest request, CancellationToken cancellationToken)
        {
            var authorId = User.GetSubjectId();

            if (authorId == null) return Unauthorized();

            var newTopic = await _topicService.CreateTopicAsync(request, authorId, cancellationToken);

            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}/{newTopic.Id}";

            var response = new
            {
                newTopic.Id,
                newTopic.Title,
                newTopic.Description,
                newTopic.AuthorId,
                newTopic.CreatedAt,
                newTopic.Status,
                newTopic.AllowComments,
                newTopic.AnonymousVote,
                Links = new object[]
                {
                    new { rel = "self", href = baseUrl },
                    new { rel = "ideas", href = $"{baseUrl}/ideas" },
                    new { rel = "update", href = baseUrl, method = "PUT" },
                    new { rel = "delete", href = baseUrl, method = "DELETE" }
                }
            };

            return CreatedAtAction(nameof(GetById), new { id = newTopic.Id }, response);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var topic = await _topicService.GetTopicByIdAsync(id, cancellationToken);

            if (topic == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;

            var baseUrl = $"{Request.Scheme}://{Request.Host}/api/v1/topics/{topic.Id}";

            var links = new List<object>
            {
                new { rel = "self", href = baseUrl },
                new { rel = "ideas", href = $"{baseUrl}/ideas" },
                new { rel = "author", href = $"{Request.Scheme}://{Request.Host}/api/v1/users/{topic.AuthorId}" }
            };

            if (userId == topic.AuthorId)
            {
                links.Add(new { rel = "update", href = baseUrl, method = "PUT" });
                links.Add(new { rel = "delete", href = baseUrl, method = "DELETE" });
            }

            var response = new
            {
                topic.Id,
                topic.Title,
                topic.Description,
                topic.AuthorId,
                topic.CreatedAt,
                topic.Status,
                topic.AllowComments,
                topic.AnonymousVote,
                Links = links.ToArray()
            };

            return Ok(response);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTopicRequest request, CancellationToken cancellationToken)
        {
            var requesterId = User.GetSubjectId();
            var requesterRole = User.GetRole();

            if (requesterId == null || string.IsNullOrWhiteSpace(requesterRole)) return Unauthorized();

            var updated = await _topicService.UpdateTopicAsync(id, request, requesterId, requesterRole, cancellationToken);

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
                updated.AllowComments,
                updated.AnonymousVote,
                Links = new object[]
                {
                    new { rel = "self", href = baseUrl },
                    new { rel = "ideas", href = $"{baseUrl}/ideas" },
                    new { rel = "author", href = $"{Request.Scheme}://{Request.Host}/api/v1/users/{updated.AuthorId}" }
                }
            };

            return Ok(response);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            var requesterId = User.GetSubjectId();
            var requesterRole = User.GetRole();

            if (requesterId == null || string.IsNullOrWhiteSpace(requesterRole)) return Unauthorized();

            await _topicService.DeleteTopicAsync(id, requesterId, requesterRole, cancellationToken);
            return NoContent();
        }
    }
}