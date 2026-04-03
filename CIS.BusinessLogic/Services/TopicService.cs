using CIS.BusinessLogic.dtos;
using CIS.DataAcces.Models;
using CIS.DataAcces.Data;

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
}