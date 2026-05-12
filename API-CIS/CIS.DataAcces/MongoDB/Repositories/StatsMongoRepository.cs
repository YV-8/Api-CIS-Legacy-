using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Data;
using CIS.DataAcces.MongoDB.Documents;
using MongoDB.Driver;

namespace CIS.DataAcces.MongoDB.Repositories;

public class StatsMongoRepository : IStatsRepository
{
    private readonly IMongoCollection<TopicDocument>    _topics;
    private readonly IMongoCollection<IdeaDocument>     _ideas;
    private readonly IMongoCollection<CommentDocument>  _comments;

    public StatsMongoRepository(MongoDbContext context)
    {
        _topics   = context.GetCollection<TopicDocument>("topics");
        _ideas    = context.GetCollection<IdeaDocument>("ideas");
        _comments = context.GetCollection<CommentDocument>("comments");
    }

    public async Task<IReadOnlyList<TopTopicStatsResponse>> GetTopTopicsByActivityAsync(
        int? limit,
        CancellationToken cancellationToken = default)
    {
        var take = limit ?? 10;

        var activeIdeas = await _ideas
            .Find(i => i.DeletedAt == null)
            .Project(i => new { i.Id, i.TopicId, i.VoteCount })
            .ToListAsync(cancellationToken);

        var ideaIdToTopicId = activeIdeas
            .ToDictionary(i => i.Id, i => i.TopicId);

        var allComments = await _comments
            .Find(_ => true)
            .Project(c => new { c.IdeaId })
            .ToListAsync(cancellationToken);

        var commentsByTopic = new Dictionary<string, int>();
        foreach (var c in allComments)
        {
            if (!ideaIdToTopicId.TryGetValue(c.IdeaId, out var topicId)) continue;
            commentsByTopic.TryAdd(topicId, 0);
            commentsByTopic[topicId]++;
        }

        var statsByTopic = activeIdeas
            .GroupBy(i => i.TopicId)
            .ToDictionary(
                g => g.Key,
                g => new { IdeaCount = g.Count(), TotalVotes = g.Sum(i => i.VoteCount) }
            );

        var topics = await _topics.Find(_ => true).ToListAsync(cancellationToken);

        return topics
            .Select(t =>
            {
                statsByTopic.TryGetValue(t.Id, out var stat);
                return new TopTopicStatsResponse
                {
                    TopicId    = t.Id,
                    Title      = t.Title,
                    IdeaCount  = stat?.IdeaCount ?? 0,
                    TotalVotes = stat?.TotalVotes ?? 0
                };
            })
            .OrderByDescending(t => t.IdeaCount + t.TotalVotes)
            .Take(take)
            .ToList();
    }

    public async Task<IReadOnlyList<TopIdeaResponse>> GetTopIdeasAsync(
        string? topicId,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        var take = limit ?? 10;

        var filter = Builders<IdeaDocument>.Filter.Eq(i => i.DeletedAt, null);

        if (!string.IsNullOrEmpty(topicId))
            filter &= Builders<IdeaDocument>.Filter.Eq(i => i.TopicId, topicId);

        var ideas = await _ideas
            .Find(filter)
            .Sort(Builders<IdeaDocument>.Sort.Descending(i => i.VoteCount))
            .Limit(take)
            .ToListAsync(cancellationToken);

        return ideas.Select(i => new TopIdeaResponse(
            IdeaId:    i.Id,
            Title:     i.Title,
            TopicId:   i.TopicId,
            AuthorId:  i.AuthorId,
            VoteCount: i.VoteCount
        )).ToList();
    }

    public async Task<IReadOnlyList<TopUserResponse>> GetTopUsersAsync(
        int? limit,
        CancellationToken cancellationToken = default)
    {
        var take = limit ?? 10;

        var ideas = await _ideas
            .Find(i => i.DeletedAt == null)
            .Project(i => new { i.AuthorId })
            .ToListAsync(cancellationToken);

        var comments = await _comments
            .Find(_ => true)
            .Project(c => new { c.UserId })
            .ToListAsync(cancellationToken);

        var activity = new Dictionary<string, int>();

        foreach (var idea in ideas)
        {
            if (string.IsNullOrEmpty(idea.AuthorId)) continue;
            activity.TryAdd(idea.AuthorId, 0);
            activity[idea.AuthorId]++;
        }

        foreach (var comment in comments)
        {
            if (string.IsNullOrEmpty(comment.UserId)) continue;
            activity.TryAdd(comment.UserId, 0);
            activity[comment.UserId]++;
        }

        return activity
            .OrderByDescending(kv => kv.Value)
            .Take(take)
            .Select(kv => new TopUserResponse(
                UserId:        kv.Key,
                ActivityCount: kv.Value
            ))
            .ToList();
    }
}