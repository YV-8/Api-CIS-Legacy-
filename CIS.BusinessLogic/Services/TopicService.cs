using CIS.BusinessLogic.dtos;
using CIS.DataAcces.Models;
using CIS.DataAcces.Data;
using Microsoft.EntityFrameworkCore;

namespace CIS.BusinessLogic.Services;
public class TopicService : ITopicService
{
    private readonly CisDbContext _context;

    public TopicService(CisDbContext context)
    {
        _context = context;
    }

    public async Task<Topic> CreateTopicAsync(CreateTopicRequest request, string authorId)
    {
        if (string.IsNullOrEmpty(authorId))
            throw new ArgumentException("AuthorId is required", nameof(authorId));

        var topic = new Topic
        {
            Title = request.Title,
            Description = request.Description,
            AuthorId = authorId,
            Type = TopicType.other,
            Status = TopicStatus.draft,
            VoteType = "single",
        };

        _context.Topics.Add(topic);
        await _context.SaveChangesAsync();

        return topic;
    }

    public async Task<PaginatedResponse<TopicResponse>> GetTopicsAsync(int page, int size, string? authorId, DateTime? createdFrom, DateTime? createdTo, string? sort)
    {
        var query = _context.Topics.AsQueryable();

        // Filters
        if (!string.IsNullOrEmpty(authorId))
        {
            query = query.Where(t => t.AuthorId == authorId);
        }

        if (createdFrom.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= createdFrom.Value);
        }

        if (createdTo.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= createdTo.Value);
        }

        // Sorting
        if (!string.IsNullOrEmpty(sort))
        {
            var sortParts = sort.Split(',');
            var sortBy = sortParts[0];
            var sortOrder = sortParts.Length > 1 ? sortParts[1] : "asc";

            query = sortBy.ToLower() switch
            {
                "createdat" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
                _ => query.OrderBy(t => t.CreatedAt) // default
            };
        }
        else
        {
            query = query.OrderBy(t => t.CreatedAt);
        }

        // Pagination
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero.");
        }

        var totalElements = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalElements / size);

        var topics = await query
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        var content = topics.Select(t => new TopicResponse
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            AuthorId = t.AuthorId,
            CreatedAt = t.CreatedAt,
            Status = t.Status,
            Links = new object[]
            {
                new { rel = "self", href = $"/api/v1/topics/{t.Id}" }
            }
        });

        return new PaginatedResponse<TopicResponse>
        {
            Content = content,
            Page = page,
            Size = size,
            TotalElements = totalElements,
            TotalPages = totalPages
        };
    }

    public async Task<Topic?> GetTopicByIdAsync(string id)
    {
        return await _context.Topics.FindAsync(id);
    }

    public async Task<Topic> UpdateTopicAsync(string id, UpdateTopicRequest request, string requesterId)
    {
        if (string.IsNullOrEmpty(requesterId))
            throw new ArgumentException("RequesterId is required", nameof(requesterId));

        var topic = await _context.Topics.FindAsync(id);
        if (topic == null)
            throw new KeyNotFoundException("Topic not found");

        if (topic.AuthorId != requesterId)
            throw new UnauthorizedAccessException("Not allowed to update this topic");

        topic.Title = request.Title;
        topic.Description = request.Description;
        topic.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return topic;
    }

    public async Task DeleteTopicAsync(string id, string requesterId)
    {
        if (string.IsNullOrEmpty(requesterId))
            throw new ArgumentException("RequesterId is required", nameof(requesterId));

        var topic = await _context.Topics.FindAsync(id);
        if (topic == null)
            throw new KeyNotFoundException("Topic not found");

        if (topic.AuthorId != requesterId)
            throw new UnauthorizedAccessException("Not allowed to delete this topic");

        _context.Topics.Remove(topic);
        await _context.SaveChangesAsync();
    }
}