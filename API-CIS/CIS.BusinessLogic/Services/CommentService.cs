using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Exceptions;
using CIS.BusinessLogic.Persistence;

namespace CIS.BusinessLogic.Services;

public class CommentService : ICommentService
{
    private readonly ITopicRepository _topics;
    private readonly IIdeaRepository _ideas;
    private readonly ICommentRepository _comments;

    public CommentService(ITopicRepository topics, IIdeaRepository ideas, ICommentRepository comments)
    {
        _topics = topics;
        _ideas = ideas;
        _comments = comments;
    }

    public async Task<CommentResponse> CreateCommentAsync(
        string topicId,
        string ideaId,
        CreateCommentRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var topic = await _topics.FindActiveByIdAsync(topicId, cancellationToken);
        if (topic == null)
            throw new NotFoundException("Topic not found");

        if (!topic.AllowComments)
            throw new ForbiddenException("Comments are not allowed for this topic");

        var idea = await _ideas.FindInTopicReadAsync(topicId, ideaId, cancellationToken);
        if (idea == null)
            throw new NotFoundException("Idea not found");

        var comment = await _comments.InsertAsync(
            new CommentInsertData(request.Content, ideaId, userId),
            cancellationToken);

        return MapCommentResponse(topicId, comment);
    }

    public async Task<PaginatedResponse<CommentResponse>> GetCommentsAsync(
        string topicId,
        string ideaId,
        int page,
        int size,
        string[]? sort,
        CancellationToken cancellationToken = default)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero.");

        var topic = await _topics.FindActiveByIdAsync(topicId, cancellationToken);
        if (topic == null)
            throw new NotFoundException("Topic not found");

        var idea = await _ideas.FindInTopicReadAsync(topicId, ideaId, cancellationToken);
        if (idea == null)
            throw new NotFoundException("Idea not found");

        var (items, totalElements) = await _comments.GetPagedForIdeaAsync(ideaId, page, size, sort, cancellationToken);
        var totalPages = totalElements == 0 ? 0 : (int)Math.Ceiling((double)totalElements / size);

        return new PaginatedResponse<CommentResponse>
        {
            Content = items.Select(item => MapCommentResponse(topicId, item)).ToList(),
            Page = page,
            Size = size,
            TotalElements = totalElements,
            TotalPages = totalPages
        };
    }

    private static CommentResponse MapCommentResponse(string topicId, CommentDetails comment)
    {
        return new CommentResponse
        {
            Id = comment.Id,
            Content = comment.Content,
            IdeaId = comment.IdeaId,
            UserId = comment.UserId,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            Links = new object[]
            {
                new { rel = "self", href = $"/api/v1/topics/{topicId}/ideas/{comment.IdeaId}/comments/{comment.Id}" },
                new { rel = "idea", href = $"/api/v1/topics/{topicId}/ideas/{comment.IdeaId}" }
            }
        };
    }
}
