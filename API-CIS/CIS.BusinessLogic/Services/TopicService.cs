using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Exceptions;
using CIS.BusinessLogic.Persistence;
using System.Threading;

namespace CIS.BusinessLogic.Services;

public class TopicService : ITopicService
{
    private static readonly HashSet<string> PrivilegedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADMIN",
        "OWNER"
    };

    private readonly ITopicRepository _topics;

    public TopicService(ITopicRepository topics)
    {
        _topics = topics;
    }

    public async Task<TopicDetails> CreateTopicAsync(CreateTopicRequest request, string authorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(authorId))
            throw new ArgumentException("AuthorId is required", nameof(authorId));

        return await _topics.InsertAsync(request, authorId, cancellationToken);
    }

    public async Task<PaginatedResponse<TopicResponse>> GetTopicsAsync(int page, int size, string? authorId, DateTime? createdFrom, DateTime? createdTo, string[]? sort, CancellationToken cancellationToken = default)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero.");

        var (items, totalElements) = await _topics.GetPagedAsync(page, size, authorId, createdFrom, createdTo, sort, cancellationToken);
        var totalPages = (int)Math.Ceiling((double)totalElements / size);

        var content = items.Select(t => new TopicResponse
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            AuthorId = t.AuthorId,
            CreatedAt = t.CreatedAt,
            Status = t.Status,
            AllowComments = t.AllowComments,
            AnonymousVote = t.AnonymousVote,
            Links = new object[]
            {
                new { rel = "self", href = $"/api/v1/topics/{t.Id}" }
            }
        });

        return new PaginatedResponse<TopicResponse>
        {
            Content = content,
            Page = page,
            Size = size,
            TotalElements = totalElements,
            TotalPages = totalPages
        };
    }

    public Task<TopicDetails?> GetTopicByIdAsync(string id, CancellationToken cancellationToken = default) =>
        _topics.FindActiveByIdAsync(id, cancellationToken);

    public async Task<TopicDetails> UpdateTopicAsync(string id, UpdateTopicRequest request, string requesterId, string requesterRole, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(requesterId))
            throw new ArgumentException("RequesterId is required", nameof(requesterId));

        var topic = await _topics.FindActiveByIdAsync(id, cancellationToken);
        if (topic == null)
            throw new NotFoundException("Topic not found");

        if (topic.AuthorId != requesterId && !PrivilegedRoles.Contains(requesterRole))
            throw new ForbiddenException("Not allowed to update this topic");

        var updated = await _topics.TryUpdateAsync(id, request, cancellationToken);
        return updated ?? throw new NotFoundException("Topic not found");
    }

    public async Task DeleteTopicAsync(string id, string requesterId, string requesterRole, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(requesterId))
            throw new ArgumentException("RequesterId is required", nameof(requesterId));

        var topic = await _topics.FindActiveByIdAsync(id, cancellationToken);
        if (topic == null)
            throw new NotFoundException("Topic not found");

        if (topic.AuthorId != requesterId && !PrivilegedRoles.Contains(requesterRole))
            throw new ForbiddenException("Not allowed to delete this topic");

        var deleted = await _topics.TrySoftDeleteAsync(id, cancellationToken);
        if (!deleted)
            throw new NotFoundException("Topic not found");
    }
}
