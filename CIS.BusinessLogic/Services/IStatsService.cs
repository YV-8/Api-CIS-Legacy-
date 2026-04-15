using CIS.BusinessLogic.dtos;

namespace CIS.BusinessLogic.Services;

public interface IStatsService
{
    Task<IReadOnlyCollection<TopTopicStatsResponse>> GetTopTopicsByActivityAsync(int? limit = null);
}
