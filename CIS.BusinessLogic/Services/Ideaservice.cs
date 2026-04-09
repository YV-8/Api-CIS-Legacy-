using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Exceptions;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using Microsoft.EntityFrameworkCore;

namespace CIS.BusinessLogic.Services;

public class IdeaService : IIdeaService
{
    private readonly CisDbContext _context;

    public IdeaService(CisDbContext context)
    {
        _context = context;
    }

    public async Task<IdeaResponse> CreateIdeaAsync(string topicId, CreateIdeaRequest request, string authorId)
    {
        var topicExists = await _context.Topics
            .AnyAsync(t => t.Id == topicId && t.DeletedAt == null);

        if (!topicExists)
            throw new NotFoundException("Topic not found");

        var idea = new Idea
        {
            Title = request.Title,
            Description = request.Description,
            TopicId = topicId,
            AuthorId = authorId,
            VoteCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Ideas.Add(idea);
        await _context.SaveChangesAsync();

        return MapIdeaResponse(idea);
    }

    public async Task<PaginatedResponse<IdeaResponse>> GetIdeasAsync(
        string topicId,
        int page,
        int size,
        string? authorId,
        string? sort)
    {
        var topicExists = await _context.Topics
            .AnyAsync(t => t.Id == topicId && t.DeletedAt == null);

        if (!topicExists)
            throw new NotFoundException("Topic not found");

        var query = _context.Ideas
            .AsNoTracking()
            .Where(i => i.TopicId == topicId);

        if (!string.IsNullOrWhiteSpace(authorId))
        {
            query = query.Where(i => i.AuthorId == authorId);
        }

        query = ApplySorting(query, sort);

        var totalElements = await query.CountAsync();
        var totalPages = totalElements == 0 ? 0 : (int)Math.Ceiling((double)totalElements / size);

        var ideas = await query
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        return new PaginatedResponse<IdeaResponse>
        {
            Content = ideas.Select(MapIdeaResponse).ToList(),
            Page = page,
            Size = size,
            TotalElements = totalElements,
            TotalPages = totalPages
        };
    }

    public async Task<IdeaResponse> UpdateIdeaAsync(
        string topicId,
        string ideaId,
        UpdateIdeaRequest request,
        string currentUserId)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == ideaId && i.TopicId == topicId);

        if (idea == null)
        {
            var topicExists = await _context.Topics
                .AnyAsync(t => t.Id == topicId && t.DeletedAt == null);

            if (!topicExists)
                throw new NotFoundException("Topic not found");

            throw new NotFoundException("Idea not found");
        }

        if (idea.AuthorId != currentUserId)
            throw new ForbiddenException("You are not allowed to modify this idea");

        idea.Title = request.Title;
        idea.Description = request.Description;
        idea.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapIdeaResponse(idea);
    }

   public async Task DeleteIdeaAsync(string topicId, string ideaId, string currentUserId)
{
    var idea = await _context.Ideas
        .Include(i => i.Votes)
        .FirstOrDefaultAsync(i => i.Id == ideaId && i.TopicId == topicId);

    if (idea == null)
    {
        var topicExists = await _context.Topics
            .AnyAsync(t => t.Id == topicId && t.DeletedAt == null);

        if (!topicExists)
            throw new NotFoundException("Topic not found");

        throw new NotFoundException("Idea not found");
    }

    if (idea.AuthorId != currentUserId)
        throw new ForbiddenException("You are not allowed to delete this idea");

    if (idea.Votes.Any())
    {
        _context.Votes.RemoveRange(idea.Votes);
    }

    _context.Ideas.Remove(idea);

    var affectedRows = await _context.SaveChangesAsync();

    if (affectedRows == 0)
        throw new Exception("Idea was not deleted");
}

    private static IQueryable<Idea> ApplySorting(IQueryable<Idea> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return query.OrderByDescending(i => i.VoteCount).ThenByDescending(i => i.CreatedAt);

        var parts = sort.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var sortBy = parts.ElementAtOrDefault(0)?.ToLowerInvariant() ?? "votecount";
        var direction = parts.ElementAtOrDefault(1)?.ToLowerInvariant() ?? "desc";

        return (sortBy, direction) switch
        {
            ("votecount", "asc") => query.OrderBy(i => i.VoteCount).ThenByDescending(i => i.CreatedAt),
            ("votecount", "desc") => query.OrderByDescending(i => i.VoteCount).ThenByDescending(i => i.CreatedAt),
            ("createdat", "asc") => query.OrderBy(i => i.CreatedAt),
            ("createdat", "desc") => query.OrderByDescending(i => i.CreatedAt),
            _ => query.OrderByDescending(i => i.VoteCount).ThenByDescending(i => i.CreatedAt)
        };
    }

    private static IdeaResponse MapIdeaResponse(Idea idea)
    {
        return new IdeaResponse
        {
            Id = idea.Id,
            Title = idea.Title,
            Description = idea.Description,
            AuthorId = idea.AuthorId,
            TopicId = idea.TopicId,
            VoteCount = idea.VoteCount,
            CreatedAt = idea.CreatedAt,
            Links = new object[]
            {
                new { rel = "self", href = $"/api/v1/topics/{idea.TopicId}/ideas/{idea.Id}" },
                new { rel = "topic", href = $"/api/v1/topics/{idea.TopicId}" },
                new { rel = "vote", href = $"/api/v1/topics/{idea.TopicId}/ideas/{idea.Id}/vote" },
                new { rel = "author", href = $"/api/v1/users/{idea.AuthorId}" }
            }
        };
    }

    private static object[] BuildCollectionLinks(string topicId, int page, int size, int totalPages)
    {
        var baseUrl = $"/api/v1/topics/{topicId}/ideas";
        var links = new List<object>
        {
            new { rel = "self", href = $"{baseUrl}?page={page}&size={size}" }
        };

        if (page > 0)
        {
            links.Add(new { rel = "first", href = $"{baseUrl}?page=0&size={size}" });
            links.Add(new { rel = "prev", href = $"{baseUrl}?page={page - 1}&size={size}" });
        }

        if (page < totalPages - 1)
        {
            links.Add(new { rel = "next", href = $"{baseUrl}?page={page + 1}&size={size}" });
            links.Add(new { rel = "last", href = $"{baseUrl}?page={totalPages - 1}&size={size}" });
        }

        return links.ToArray();
    }
}