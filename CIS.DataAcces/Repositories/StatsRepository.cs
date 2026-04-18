using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Data;
using Microsoft.EntityFrameworkCore;

namespace CIS.DataAcces.Repositories;

public class StatsRepository : IStatsRepository
{
    private readonly CisDbContext _context;

    public StatsRepository(CisDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TopTopicStatsResponse>> GetTopTopicsByActivityAsync(int? limit, CancellationToken cancellationToken = default)
    {
        IQueryable<TopTopicStatsResponse> query = _context.Topics
            .AsNoTracking()
            .Where(topic => topic.DeletedAt == null)
            .Select(topic => new TopTopicStatsResponse
            {
                TopicId = topic.Id,
                Title = topic.Title,
                IdeaCount = topic.Ideas.Count(idea => idea.DeletedAt == null),
                TotalVotes = topic.Ideas.Where(idea => idea.DeletedAt == null).Sum(idea => (int?)idea.VoteCount) ?? 0
            });

        query = query
            .OrderByDescending(topic => topic.IdeaCount)
            .ThenByDescending(topic => topic.TotalVotes)
            .ThenBy(topic => topic.Title);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TopIdeaResponse>> GetTopIdeasAsync(string? topicId, int? limit, CancellationToken cancellationToken = default)
    {
        var query = _context.Ideas
            .AsNoTracking()
            .Where(i => i.DeletedAt == null && i.VoteCount > 0);

        if (!string.IsNullOrWhiteSpace(topicId))
            query = query.Where(i => i.TopicId == topicId);

        var orderedQuery = query
            .OrderByDescending(i => i.VoteCount)
            .ThenByDescending(i => i.CreatedAt);

        if (limit.HasValue)
            return await orderedQuery
                .Take(limit.Value)
                .Select(i => new TopIdeaResponse(i.Id, i.Title, i.TopicId, i.AuthorId, i.VoteCount))
                .ToListAsync(cancellationToken);

        return await orderedQuery
            .Select(i => new TopIdeaResponse(i.Id, i.Title, i.TopicId, i.AuthorId, i.VoteCount))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TopUserResponse>> GetTopUsersAsync(int? limit, CancellationToken cancellationToken = default)
    {
        var ideasCount = await _context.Ideas
            .Where(i => i.DeletedAt == null)
            .GroupBy(i => i.AuthorId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var votesCount = await _context.Votes
            .Where(v => v.UserId != null)
            .GroupBy(v => v.UserId)
            .Select(g => new { UserId = g.Key!, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var activityMap = new Dictionary<string, int>();
        foreach (var idea in ideasCount)
            activityMap[idea.UserId] = idea.Count;

        foreach (var vote in votesCount)
        {
            if (activityMap.ContainsKey(vote.UserId))
                activityMap[vote.UserId] += vote.Count;
            else
                activityMap[vote.UserId] = vote.Count;
        }

        var sortedResult = activityMap
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new TopUserResponse(kv.Key, kv.Value))
            .ToList();

        if (limit.HasValue)
            return sortedResult.Take(limit.Value).ToList();

        return sortedResult;
    }
}
