using CIS.BusinessLogic.dtos;

namespace CIS.BusinessLogic.Persistence;

public interface IStatsRepository
{
    Task<IReadOnlyList<TopTopicStatsResponse>> GetTopTopicsByActivityAsync(int? limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopIdeaResponse>> GetTopIdeasAsync(string? topicId, int? limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopUserResponse>> GetTopUsersAsync(int? limit, CancellationToken cancellationToken = default);
}
