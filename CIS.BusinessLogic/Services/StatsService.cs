using CIS.BusinessLogic.dtos;
using CIS.DataAcces.Data;
using Microsoft.EntityFrameworkCore;

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
}
