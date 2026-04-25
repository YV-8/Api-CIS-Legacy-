using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Data;
using CIS.DataAcces.MongoDB.Documents;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CIS.DataAcces.MongoDB.Repositories;

public class StatsMongoRepository : IStatsRepository
{
    private readonly IMongoCollection<TopicDocument>   _topics;
    private readonly IMongoCollection<IdeaDocument>    _ideas;
    private readonly IMongoCollection<CommentDocument> _comments;
    private readonly IMongoCollection<VoteDocument> _votes;

    public StatsMongoRepository(MongoDbContext context)
    {
        _topics   = context.GetCollection<TopicDocument>("topics");
        _ideas    = context.GetCollection<IdeaDocument>("ideas");
        _comments = context.GetCollection<CommentDocument>("comments");
        _votes    = context.GetCollection<VoteDocument>("votes");
    }

    // Top Topics: los que tienen más ideas + votos + comentarios
    public async Task<IReadOnlyList<TopTopicStatsResponse>> GetTopTopicsByActivityAsync(
        int? limit,
        CancellationToken cancellationToken = default)
    {
        var take = limit ?? 10;

        // Agrupar ideas por topicId y contar
        var ideaPipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("DeletedAt", BsonNull.Value)),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$TopicId" },
                { "ideaCount", new BsonDocument("$sum", 1) },
                { "totalVotes", new BsonDocument("$sum", "$VoteCount") }
            })
        };

        var ideaStats = await _ideas
            .Aggregate<IdeaStatGroup>(
                PipelineDefinition<IdeaDocument, IdeaStatGroup>
                    .Create(ideaPipeline))
            .ToListAsync(cancellationToken);

        // Comentarios por idea → necesitamos relacionar idea→topic
        // Lo resolvemos con un lookup en memoria (colecciones pequeñas de stats)
        var ideaIds = (await _ideas
            .Find(i => i.DeletedAt == null)
            .Project(i => new { i.Id, i.TopicId })
            .ToListAsync(cancellationToken))
            .ToDictionary(i => i.Id, i => i.TopicId);

        var commentGroups = await _comments
            .Aggregate<CommentStatGroup>(
                PipelineDefinition<CommentDocument, CommentStatGroup>
                    .Create(new[]
                    {
                        new BsonDocument("$group", new BsonDocument
                        {
                            { "_id", "$IdeaId" },
                            { "count", new BsonDocument("$sum", 1) }
                        })
                    }))
            .ToListAsync(cancellationToken);

        // Sumar comentarios por topic
        var commentsByTopic = new Dictionary<Guid, int>();
        foreach (var cg in commentGroups)
        {
            if (ideaIds.TryGetValue(cg.Id, out var topicId))
            {
                commentsByTopic.TryAdd(topicId, 0);
                commentsByTopic[topicId] += cg.Count;
            }
        }

        // Unir con los topics
        var topics = await _topics.Find(_ => true).ToListAsync(cancellationToken);
        var statDict = ideaStats.ToDictionary(s => s.Id);

        var result = topics
            .Select(t =>
            {
                statDict.TryGetValue(t.Id, out var stat);
                commentsByTopic.TryGetValue(t.Id, out var comments);

                return new TopTopicStatsResponse
                {
                    TopicId      = t.Id.ToString(),
                    Title        = t.Title,
                    IdeaCount    = stat?.IdeaCount ?? 0,
                    TotalVotes   = stat?.TotalVotes ?? 0
                };
            })
            .OrderByDescending(t => t.IdeaCount + t.TotalVotes)
            .Take(take)
            .ToList();

        return result;
    }

    // Top Ideas: las más votadas, opcionalmente filtradas por topic
    public async Task<IReadOnlyList<TopIdeaResponse>> GetTopIdeasAsync(
        string? topicId,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        var take = limit ?? 10;

        var filter = Builders<IdeaDocument>.Filter.Eq(i => i.DeletedAt, null);

        if (!string.IsNullOrEmpty(topicId) && Guid.TryParse(topicId, out var gTopicId))
            filter &= Builders<IdeaDocument>.Filter.Eq(i => i.TopicId, gTopicId);

        var ideas = await _ideas
            .Find(filter)
            .Sort(Builders<IdeaDocument>.Sort.Descending(i => i.VoteCount))
            .Limit(take)
            .ToListAsync(cancellationToken);

        return ideas.Select(i => new TopIdeaResponse(
            IdeaId:   i.Id.ToString(),
            Title:    i.Title,
            TopicId:  i.TopicId.ToString(),
            AuthorId: i.AuthorId,
            VoteCount: i.VoteCount
        )).ToList();
    }

    // Top Users: los que han creado más ideas
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

    // ─── Clases internas para deserializar aggregations ──────────
    private class IdeaStatGroup
    {
        [BsonId]
        public Guid Id { get; set; }
        public int IdeaCount  { get; set; }
        public int TotalVotes { get; set; }
    }

    private class CommentStatGroup
    {
        [BsonId]
        public Guid Id { get; set; }
        public int Count { get; set; }
    }

    private class UserStatGroup
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;
        public int IdeaCount  { get; set; }
        public int TotalVotes { get; set; }
    }
}