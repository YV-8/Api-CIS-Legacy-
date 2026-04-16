using CIS.BusinessLogic.Domain;

namespace CIS.BusinessLogic.Persistence;

public interface ICommentRepository
{
    Task<CommentDetails> InsertAsync(CommentInsertData data, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CommentDetails> Items, int TotalElements)> GetPagedForIdeaAsync(
        string ideaId,
        int page,
        int size,
        string[]? sort,
        CancellationToken cancellationToken = default);
}
