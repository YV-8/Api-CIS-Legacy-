using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Exceptions;
using CIS.BusinessLogic.Persistence;
using System.Threading;

namespace CIS.BusinessLogic.Services;

public class StatsService : IStatsService
{
    private readonly IStatsRepository _stats;
    private readonly ITopicRepository _topics;

    public StatsService(IStatsRepository stats, ITopicRepository topics)
    {
        _stats = stats;
        _topics = topics;
    }

    public async Task<IReadOnlyCollection<TopTopicStatsResponse>> GetTopTopicsByActivityAsync(int? limit = null, CancellationToken cancellationToken = default)
    {
        var list = await _stats.GetTopTopicsByActivityAsync(limit, cancellationToken);
        return list;
    }

    public async Task<IEnumerable<TopIdeaResponse>> GetTopIdeasAsync(string? topicId = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (limit.HasValue && limit.Value <= 0)
            return Enumerable.Empty<TopIdeaResponse>();

        if (!string.IsNullOrWhiteSpace(topicId) && !await _topics.ExistsActiveAsync(topicId, cancellationToken))
            throw new NotFoundException("Topic not found");

        return await _stats.GetTopIdeasAsync(topicId, limit, cancellationToken);
    }

    public async Task<IEnumerable<TopUserResponse>> GetTopUsersAsync(int? limit = null, CancellationToken cancellationToken = default)
    {
        var list = await _stats.GetTopUsersAsync(limit, cancellationToken);
        return list;
    }
}
