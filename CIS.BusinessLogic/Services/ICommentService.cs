using CIS.BusinessLogic.dtos;

namespace CIS.BusinessLogic.Services;

public interface ICommentService
{
    Task<CommentResponse> CreateCommentAsync(
        string topicId,
        string ideaId,
        CreateCommentRequest request,
        string userId,
        CancellationToken cancellationToken = default);

    Task<PaginatedResponse<CommentResponse>> GetCommentsAsync(
        string topicId,
        string ideaId,
        int page,
        int size,
        string[]? sort,
        CancellationToken cancellationToken = default);
}
