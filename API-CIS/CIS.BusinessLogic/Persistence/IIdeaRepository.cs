using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.dtos;

namespace CIS.BusinessLogic.Persistence;

public interface IIdeaRepository
{
    Task<IdeaDetails> InsertAsync(IdeaInsertData data, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<IdeaDetails> Items, int TotalElements)> GetPagedForTopicAsync(
        string topicId,
        int page,
        int size,
        string? authorId,
        string[]? sort,
        CancellationToken cancellationToken = default);

    Task<IdeaDetails?> FindInTopicReadAsync(string topicId, string ideaId, CancellationToken cancellationToken = default);

    Task<IdeaDetails?> TryUpdateAsync(string topicId, string ideaId, UpdateIdeaRequest request, CancellationToken cancellationToken = default);

    Task<bool> TrySoftDeleteAsync(string topicId, string ideaId, CancellationToken cancellationToken = default);
}
