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

    public async Task<Topic?> GetTopicByIdAsync(string id)
    {
        return await _context.Topics.FindAsync(id);
    }

    public async Task<Topic> UpdateTopicAsync(string id, UpdateTopicRequest request, string requesterId)
    {
        var topic = await _context.Topics.FindAsync(id)
            ?? throw new KeyNotFoundException($"Topic {id} not found");

        if (topic.AuthorId != requesterId)
            throw new UnauthorizedAccessException("Only the author can update this topic");

        topic.Title = request.Title;
        topic.Description = request.Description;
        topic.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return topic;
    }

    public async Task DeleteTopicAsync(string id, string requesterId)
    {
        var topic = await _context.Topics.FindAsync(id)
            ?? throw new KeyNotFoundException($"Topic {id} not found");

        if (topic.AuthorId != requesterId)
            throw new UnauthorizedAccessException("Only the author can delete this topic");

        _context.Topics.Remove(topic);
        await _context.SaveChangesAsync();
    }
}