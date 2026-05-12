using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Exceptions;
using CIS.BusinessLogic.Persistence;
using System.Threading;

namespace CIS.BusinessLogic.Services;

public class IdeaService : IIdeaService
{
    private static readonly HashSet<string> PrivilegedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADMIN",
        "OWNER"
    };

    private readonly IIdeaRepository _ideas;
    private readonly ITopicRepository _topics;
    private readonly IVoteRepository _votes;

    public IdeaService(IIdeaRepository ideas, ITopicRepository topics, IVoteRepository votes)
    {
        _ideas = ideas;
        _topics = topics;
        _votes = votes;
    }

    public async Task<IdeaResponse> CreateIdeaAsync(string topicId, CreateIdeaRequest request, string authorId, CancellationToken cancellationToken = default)
    {
        if (!await _topics.ExistsActiveAsync(topicId, cancellationToken))
            throw new NotFoundException("Topic not found");

        var idea = await _ideas.InsertAsync(new IdeaInsertData(request.Title, request.Description, topicId, authorId), cancellationToken);
        return MapIdeaResponse(idea);
    }

    public async Task<PaginatedResponse<IdeaResponse>> GetIdeasAsync(
        string topicId,
        int page,
        int size,
        string? authorId,
        string[]? sort,
        CancellationToken cancellationToken = default)
    {
        if (!await _topics.ExistsActiveAsync(topicId, cancellationToken))
            throw new NotFoundException("Topic not found");

        var (items, totalElements) = await _ideas.GetPagedForTopicAsync(topicId, page, size, authorId, sort, cancellationToken);
        var totalPages = totalElements == 0 ? 0 : (int)Math.Ceiling((double)totalElements / size);

        return new PaginatedResponse<IdeaResponse>
        {
            Content = items.Select(MapIdeaResponse).ToList(),
            Page = page,
            Size = size,
            TotalElements = totalElements,
            TotalPages = totalPages
        };
    }

    public async Task<IdeaResponse> UpdateIdeaAsync(
        string topicId,
        string ideaId,
        UpdateIdeaRequest request,
        string currentUserId,
        string currentUserRole,
        CancellationToken cancellationToken = default)
    {
        if (!await _topics.ExistsActiveAsync(topicId, cancellationToken))
            throw new NotFoundException("Topic not found");

        var existing = await _ideas.FindInTopicReadAsync(topicId, ideaId, cancellationToken);
        if (existing == null)
            throw new NotFoundException("Idea not found");

        if (existing.AuthorId != currentUserId && !PrivilegedRoles.Contains(currentUserRole))
            throw new ForbiddenException("You are not allowed to modify this idea");

        var updated = await _ideas.TryUpdateAsync(topicId, ideaId, request, cancellationToken);
        if (updated == null)
            throw new NotFoundException("Idea not found");

        return await MapIdeaResponseWithVoteLinksAsync(updated, currentUserId, cancellationToken);
    }

    public async Task DeleteIdeaAsync(string topicId, string ideaId, string currentUserId, string currentUserRole, CancellationToken cancellationToken = default)
    {
        if (!await _topics.ExistsActiveAsync(topicId, cancellationToken))
            throw new NotFoundException("Topic not found");

        var existing = await _ideas.FindInTopicReadAsync(topicId, ideaId, cancellationToken);
        if (existing == null)
            throw new NotFoundException("Idea not found");

        if (existing.AuthorId != currentUserId && !PrivilegedRoles.Contains(currentUserRole))
            throw new ForbiddenException("You are not allowed to delete this idea");

        var deleted = await _ideas.TrySoftDeleteAsync(topicId, ideaId, cancellationToken);
        if (!deleted)
            throw new Exception("Idea was not deleted");
    }

    private static IdeaResponse MapIdeaResponse(IdeaDetails idea)
    {
        return new IdeaResponse
        {
            Id = idea.Id,
            Title = idea.Title,
            Description = idea.Description,
            AuthorId = idea.AuthorId,
            TopicId = idea.TopicId,
            VoteCount = idea.VoteCount,
            CreatedAt = idea.CreatedAt,
            Links = new object[]
            {
                new { rel = "self", href = $"/api/v1/topics/{idea.TopicId}/ideas/{idea.Id}" },
                new { rel = "topic", href = $"/api/v1/topics/{idea.TopicId}" },
                new { rel = "vote", href = $"/api/v1/ideas/{idea.Id}/votes" },
                new { rel = "author", href = $"/api/v1/users/{idea.AuthorId}" }
            }
        };
    }

    private async Task<IdeaResponse> MapIdeaResponseWithVoteLinksAsync(IdeaDetails idea, string? currentUserId, CancellationToken cancellationToken)
    {
        var links = new List<object>
        {
            new { rel = "self", href = $"/api/v1/topics/{idea.TopicId}/ideas/{idea.Id}" },
            new { rel = "topic", href = $"/api/v1/topics/{idea.TopicId}" },
            new { rel = "author", href = $"/api/v1/users/{idea.AuthorId}" }
        };

        if (!string.IsNullOrEmpty(currentUserId))
        {
            var hasVoted = await _votes.HasUserVotedAsync(idea.Id, currentUserId, cancellationToken);
            if (hasVoted)
                links.Add(new { rel = "unvote", href = $"/api/v1/ideas/{idea.Id}/votes", method = "DELETE" });
            else
                links.Add(new { rel = "vote", href = $"/api/v1/ideas/{idea.Id}/votes", method = "POST" });
        }
        else
        {
            links.Add(new { rel = "vote", href = $"/api/v1/ideas/{idea.Id}/votes", method = "POST" });
        }

        return new IdeaResponse
        {
            Id = idea.Id,
            Title = idea.Title,
            Description = idea.Description,
            AuthorId = idea.AuthorId,
            TopicId = idea.TopicId,
            VoteCount = idea.VoteCount,
            CreatedAt = idea.CreatedAt,
            Links = links.ToArray()
        };
    }
}
