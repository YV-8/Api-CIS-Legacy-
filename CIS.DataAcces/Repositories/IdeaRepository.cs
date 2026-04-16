using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Helpers;
using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using Microsoft.EntityFrameworkCore;

namespace CIS.DataAcces.Repositories;

public class IdeaRepository : IIdeaRepository
{
    private readonly CisDbContext _context;

    public IdeaRepository(CisDbContext context)
    {
        _context = context;
    }

    public async Task<IdeaDetails> InsertAsync(IdeaInsertData data, CancellationToken cancellationToken = default)
    {
        var idea = new Idea
        {
            Title = data.Title,
            Description = data.Description,
            TopicId = data.TopicId,
            AuthorId = data.AuthorId,
            VoteCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Ideas.Add(idea);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(idea);
    }

    public async Task<(IReadOnlyList<IdeaDetails> Items, int TotalElements)> GetPagedForTopicAsync(
        string topicId,
        int page,
        int size,
        string? authorId,
        string[]? sort,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Ideas
            .AsNoTracking()
            .Where(i => i.TopicId == topicId && i.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(authorId))
            query = query.Where(i => i.AuthorId == authorId);

        query = ApplySorting(query, sort);

        var totalElements = await query.CountAsync(cancellationToken);
        var ideas = await query
            .Skip(page * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (ideas.Select(Map).ToList(), totalElements);
    }

    public async Task<IdeaDetails?> FindInTopicReadAsync(string topicId, string ideaId, CancellationToken cancellationToken = default)
    {
        var idea = await _context.Ideas
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == ideaId && i.TopicId == topicId && i.DeletedAt == null, cancellationToken);

        return idea == null ? null : Map(idea);
    }

    public async Task<IdeaDetails?> TryUpdateAsync(string topicId, string ideaId, UpdateIdeaRequest request, CancellationToken cancellationToken = default)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == ideaId && i.TopicId == topicId && i.DeletedAt == null, cancellationToken);

        if (idea == null)
            return null;

        idea.Title = request.Title;
        idea.Description = request.Description;
        idea.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Map(idea);
    }

    public async Task<bool> TrySoftDeleteAsync(string topicId, string ideaId, CancellationToken cancellationToken = default)
    {
        var idea = await _context.Ideas
            .Include(i => i.Votes)
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.Id == ideaId && i.TopicId == topicId && i.DeletedAt == null, cancellationToken);

        if (idea == null)
            return false;

        if (idea.Votes.Count > 0)
            _context.Votes.RemoveRange(idea.Votes);

        if (idea.Comments.Count > 0)
            _context.Comments.RemoveRange(idea.Comments);

        idea.DeletedAt = DateTime.UtcNow;
        idea.UpdatedAt = DateTime.UtcNow;

        var affected = await _context.SaveChangesAsync(cancellationToken);
        return affected > 0;
    }

    private static IQueryable<Idea> ApplySorting(IQueryable<Idea> query, string[]? sort)
    {
        var whitelist = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "voteCount", "VoteCount" },
            { "createdAt", "CreatedAt" }
        };

        if (sort == null || sort.Length == 0)
            return query.OrderByDescending(i => i.VoteCount).ThenByDescending(i => i.CreatedAt);

        return query.ApplySorting(sort, whitelist);
    }

    private static IdeaDetails Map(Idea idea) => new()
    {
        Id = idea.Id,
        Title = idea.Title,
        Description = idea.Description,
        TopicId = idea.TopicId,
        AuthorId = idea.AuthorId,
        VoteCount = idea.VoteCount,
        CreatedAt = idea.CreatedAt,
        UpdatedAt = idea.UpdatedAt,
        DeletedAt = idea.DeletedAt
    };
}
