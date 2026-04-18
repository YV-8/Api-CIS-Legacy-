using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Helpers;
using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using Microsoft.EntityFrameworkCore;

namespace CIS.DataAcces.Repositories;

public class TopicRepository : ITopicRepository
{
    private readonly CisDbContext _context;

    public TopicRepository(CisDbContext context)
    {
        _context = context;
    }

    public async Task<TopicDetails> InsertAsync(CreateTopicRequest request, string authorId, CancellationToken cancellationToken = default)
    {
        var topic = new Topic
        {
            Title = request.Title,
            Description = request.Description,
            AuthorId = authorId,
            Type = TopicType.other,
            Status = TopicStatus.draft,
            VoteType = "single",
            AllowComments = request.AllowComments,
            AnonymousVote = request.AnonymousVote,
        };

        _context.Topics.Add(topic);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(topic);
    }

    public async Task<(IReadOnlyList<TopicDetails> Items, int TotalElements)> GetPagedAsync(
        int page,
        int size,
        string? authorId,
        DateTime? createdFrom,
        DateTime? createdTo,
        string[]? sort,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Topics.Where(t => t.DeletedAt == null);

        if (!string.IsNullOrEmpty(authorId))
            query = query.Where(t => t.AuthorId == authorId);

        if (createdFrom.HasValue)
            query = query.Where(t => t.CreatedAt >= createdFrom.Value);

        if (createdTo.HasValue)
        {
            var endOfDay = createdTo.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(t => t.CreatedAt <= endOfDay);
        }

        var whitelist = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "createdAt", "CreatedAt" }
        };

        if (sort != null && sort.Length > 0)
            query = query.ApplySorting(sort, whitelist);
        else
            query = query.OrderByDescending(t => t.CreatedAt);

        var totalElements = await query.CountAsync(cancellationToken);
        var topics = await query
            .Skip(page * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (topics.Select(Map).ToList(), totalElements);
    }

    public async Task<TopicDetails?> FindActiveByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var topic = await _context.Topics
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt == null, cancellationToken);

        return topic == null ? null : Map(topic);
    }

    public Task<bool> ExistsActiveAsync(string topicId, CancellationToken cancellationToken = default) =>
        _context.Topics.AnyAsync(t => t.Id == topicId && t.DeletedAt == null, cancellationToken);

    public async Task<TopicDetails?> TryUpdateAsync(string id, UpdateTopicRequest request, CancellationToken cancellationToken = default)
    {
        var topic = await _context.Topics
            .FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt == null, cancellationToken);

        if (topic == null)
            return null;

        topic.Title = request.Title;
        topic.Description = request.Description;
        topic.AllowComments = request.AllowComments ?? topic.AllowComments;
        topic.AnonymousVote = request.AnonymousVote ?? topic.AnonymousVote;
        topic.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Map(topic);
    }

    public async Task<bool> TrySoftDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var topic = await _context.Topics
            .Include(t => t.Ideas)
                .ThenInclude(i => i.Votes)
            .Include(t => t.Ideas)
                .ThenInclude(i => i.Comments)
            .FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt == null, cancellationToken);

        if (topic == null)
            return false;

        var now = DateTime.UtcNow;
        topic.DeletedAt = now;
        topic.UpdatedAt = now;

        foreach (var idea in topic.Ideas.Where(i => i.DeletedAt == null))
        {
            if (idea.Votes.Count > 0)
                _context.Votes.RemoveRange(idea.Votes);

            if (idea.Comments.Count > 0)
                _context.Comments.RemoveRange(idea.Comments);

            idea.DeletedAt = now;
            idea.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static TopicDetails Map(Topic t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        AuthorId = t.AuthorId,
        Type = t.Type,
        Status = t.Status,
        VoteType = t.VoteType,
        AllowComments = t.AllowComments,
        AnonymousVote = t.AnonymousVote,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
        DeletedAt = t.DeletedAt
    };
}
