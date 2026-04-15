using CIS.BusinessLogic.dtos;

namespace CIS.BusinessLogic.Services;

public interface IStatsService
{
    Task<IReadOnlyCollection<TopTopicStatsResponse>> GetTopTopicsByActivityAsync(int? limit = null);
    Task<IEnumerable<TopIdeaResponse>> GetTopIdeasAsync(string? topicId = null, int? limit = null);
    Task<IEnumerable<TopUserResponse>> GetTopUsersAsync(int? limit = null);
}
