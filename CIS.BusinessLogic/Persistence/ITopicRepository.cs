using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.dtos;

namespace CIS.BusinessLogic.Persistence;

public interface ITopicRepository
{
    Task<TopicDetails> InsertAsync(CreateTopicRequest request, string authorId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<TopicDetails> Items, int TotalElements)> GetPagedAsync(
        int page,
        int size,
        string? authorId,
        DateTime? createdFrom,
        DateTime? createdTo,
        string[]? sort,
        CancellationToken cancellationToken = default);

    Task<TopicDetails?> FindActiveByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<bool> ExistsActiveAsync(string topicId, CancellationToken cancellationToken = default);

    /// <summary>Returns null if the topic does not exist or is not active.</summary>
    Task<TopicDetails?> TryUpdateAsync(string id, UpdateTopicRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns false if the topic does not exist or is not active.</summary>
    Task<bool> TrySoftDeleteAsync(string id, CancellationToken cancellationToken = default);
}
