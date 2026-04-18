using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.Helpers;
using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using Microsoft.EntityFrameworkCore;

namespace CIS.DataAcces.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly CisDbContext _context;

    public CommentRepository(CisDbContext context)
    {
        _context = context;
    }

    public async Task<CommentDetails> InsertAsync(CommentInsertData data, CancellationToken cancellationToken = default)
    {
        var comment = new Comment
        {
            Content = data.Content,
            IdeaId = data.IdeaId,
            UserId = data.UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(comment);
    }

    public async Task<(IReadOnlyList<CommentDetails> Items, int TotalElements)> GetPagedForIdeaAsync(
        string ideaId,
        int page,
        int size,
        string[]? sort,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Comments
            .AsNoTracking()
            .Where(c => c.IdeaId == ideaId);

        query = ApplySorting(query, sort);

        var totalElements = await query.CountAsync(cancellationToken);
        var comments = await query
            .Skip(page * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (comments.Select(Map).ToList(), totalElements);
    }

    private static IQueryable<Comment> ApplySorting(IQueryable<Comment> query, string[]? sort)
    {
        var whitelist = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "createdAt", "CreatedAt" }
        };

        if (sort == null || sort.Length == 0)
            return query.OrderByDescending(c => c.CreatedAt);

        return query.ApplySorting(sort, whitelist);
    }

    private static CommentDetails Map(Comment comment) => new()
    {
        Id = comment.Id,
        Content = comment.Content,
        IdeaId = comment.IdeaId,
        UserId = comment.UserId,
        CreatedAt = comment.CreatedAt,
        UpdatedAt = comment.UpdatedAt
    };
}
