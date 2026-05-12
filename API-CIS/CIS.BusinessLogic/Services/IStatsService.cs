using CIS.BusinessLogic.dtos;
using System.Threading;

namespace CIS.BusinessLogic.Services;

public interface IStatsService
{
    Task<IReadOnlyCollection<TopTopicStatsResponse>> GetTopTopicsByActivityAsync(int? limit = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<TopIdeaResponse>> GetTopIdeasAsync(string? topicId = null, int? limit = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<TopUserResponse>> GetTopUsersAsync(int? limit = null, CancellationToken cancellationToken = default);
}
