using CIS.BusinessLogic.dtos;
using CIS.DataAcces.Data;
using Microsoft.EntityFrameworkCore;
using CIS.BusinessLogic.Exceptions;
namespace CIS.BusinessLogic.Services;

public class StatsService : IStatsService
{
    private readonly CisDbContext _context;

    public StatsService(CisDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<TopTopicStatsResponse>> GetTopTopicsByActivityAsync(int? limit = null)
    {
        IQueryable<TopTopicStatsResponse> query = _context.Topics
            .AsNoTracking()
            .Where(topic => topic.DeletedAt == null)
            .Select(topic => new TopTopicStatsResponse
            {
                TopicId = topic.Id,
                Title = topic.Title,
                IdeaCount = topic.Ideas.Count,
                TotalVotes = topic.Ideas.Sum(idea => (int?)idea.VoteCount) ?? 0
            });

        query = query
            .OrderByDescending(topic => topic.IdeaCount)
            .ThenByDescending(topic => topic.TotalVotes)
            .ThenBy(topic => topic.Title);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<TopIdeaResponse>> GetTopIdeasAsync(string? topicId = null, int? limit = null)
    {
        if (limit.HasValue && limit.Value <= 0) return Enumerable.Empty<TopIdeaResponse>();

        var query = _context.Ideas
            .AsNoTracking()
            .Where(i => i.VoteCount > 0);

        if (!string.IsNullOrWhiteSpace(topicId))
        {
            var topicExists = await _context.Topics
                .AnyAsync(t => t.Id == topicId && t.DeletedAt == null);

            if (!topicExists) throw new NotFoundException("Topic not found");
            
            query = query.Where(i => i.TopicId == topicId);
        }

        var orderedQuery = query
            .OrderByDescending(i => i.VoteCount)
            .ThenByDescending(i => i.CreatedAt);

        if (limit.HasValue) 
            return await orderedQuery.Take(limit.Value)
                .Select(i => new TopIdeaResponse(i.Id, i.Title, i.TopicId, i.AuthorId, i.VoteCount))
                .ToListAsync();

        return await orderedQuery
            .Select(i => new TopIdeaResponse(i.Id, i.Title, i.TopicId, i.AuthorId, i.VoteCount))
            .ToListAsync();
    }
    public async Task<IEnumerable<TopUserResponse>> GetTopUsersAsync(int? limit = null)
    {
        var ideasCount = await _context.Ideas
        .GroupBy(i => i.AuthorId)
        .Select(g => new { UserId = g.Key, Count = g.Count() })
        .ToListAsync();
        var votesCount = await _context.Votes
        .Where(v => v.UserId != null)
        .GroupBy(v => v.UserId)
        .Select(g => new { UserId = g.Key!, Count = g.Count() })
        .ToListAsync();

        var activityMap = new Dictionary<string, int> ();
        foreach (var idea in ideasCount)        
        {
            activityMap[idea.UserId] = idea.Count;
        }
        foreach (var vote in votesCount)
        {
            if (activityMap.ContainsKey(vote.UserId)) activityMap[vote.UserId] += vote.Count;
            else activityMap[vote.UserId] = vote.Count;
        }
        var sortedResult = activityMap
                .OrderByDescending(kv => kv.Value)
                .Select(kv => new TopUserResponse(kv.Key, kv.Value));

        if (limit.HasValue) return sortedResult.Take(limit.Value).ToList();

        return sortedResult.ToList();
    }
}
