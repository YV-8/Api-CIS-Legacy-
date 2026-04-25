using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Data;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace CIS.DataAcces.MongoDB.Repositories;

public class IdeaMongoRepository : IIdeaRepository
{
    private readonly IMongoCollection<IdeaDocument> _ideas;

    public IdeaMongoRepository(MongoDbContext context)
    {
        _ideas = context.GetCollection<IdeaDocument>("ideas");
    }

    public async Task<IdeaDetails> InsertAsync(IdeaInsertData d, CancellationToken cancellationToken = default)
    {
        var ideaDoc = new IdeaDocument
        {
            Id = Guid.NewGuid().ToString(),
            Title = d.Title,
            Description = d.Description,
            TopicId = d.TopicId,
            AuthorId = d.AuthorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = null
        };

        await _ideas.InsertOneAsync(ideaDoc, cancellationToken: cancellationToken);
        return MapToDomain(ideaDoc);
    }

    public async Task<(IReadOnlyList<IdeaDetails> Items, int TotalElements)> GetPagedForTopicAsync(
        string topicId,
        int page,
        int size,
        string? authorId,
        string[]? sort,
        CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<IdeaDocument>.Filter;
        Guid.TryParse(topicId, out var gTopicId);
        var filter = filterBuilder.Eq(i => i.TopicId, topicId) & 
                     filterBuilder.Eq(i => i.DeletedAt, null);

        if (!string.IsNullOrWhiteSpace(authorId))
            filter &= filterBuilder.Eq(i => i.AuthorId, authorId);

        var total = (int)await _ideas.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var items = await _ideas
            .Find(filter)
            .Sort(BuildSort(sort))
            .Skip(page * size)
            .Limit(size)
            .ToListAsync(cancellationToken);

        return (items.Select(MapToDomain).ToList(), total);
    }

    public async Task<IdeaDetails?> FindInTopicReadAsync(string topicId, string ideaId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(ideaId, out var gIdeaId) || !Guid.TryParse(topicId, out var gTopicId))
            return null;

        var filter = Builders<IdeaDocument>.Filter.And(
            Builders<IdeaDocument>.Filter.Eq(i => i.Id, ideaId),
            Builders<IdeaDocument>.Filter.Eq(i => i.TopicId, topicId),
            Builders<IdeaDocument>.Filter.Eq(i => i.DeletedAt, null)
        );

        var doc = await _ideas.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return doc == null ? null : MapToDomain(doc);
    }

    public async Task<IdeaDetails?> TryUpdateAsync( string topicId, string ideaId,UpdateIdeaRequest request,CancellationToken cancellationToken = default)
    {
        var filter = Builders<IdeaDocument>.Filter.And(
            Builders<IdeaDocument>.Filter.Eq(i => i.Id, ideaId),
            Builders<IdeaDocument>.Filter.Eq(i => i.TopicId, topicId),
            Builders<IdeaDocument>.Filter.Eq(i => i.DeletedAt, null)
        );

        var updates = new List<UpdateDefinition<IdeaDocument>>();

        if (!string.IsNullOrEmpty(request.Title))
            updates.Add(Builders<IdeaDocument>.Update.Set(i => i.Title, request.Title));
        if (!string.IsNullOrEmpty(request.Description))
            updates.Add(Builders<IdeaDocument>.Update.Set(i => i.Description, request.Description));

        updates.Add(Builders<IdeaDocument>.Update.Set(i => i.UpdatedAt, DateTime.UtcNow));

        var opts = new FindOneAndUpdateOptions<IdeaDocument>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updated = await _ideas.FindOneAndUpdateAsync(
            filter,
            Builders<IdeaDocument>.Update.Combine(updates),
            opts, cancellationToken);

        return updated is null ? null : MapToDomain(updated);
    }

    public async Task<bool> TrySoftDeleteAsync(string topicId, string ideaId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(ideaId, out var gIdeaId) || !Guid.TryParse(topicId, out var gTopicId))
            return false;

        var filter = Builders<IdeaDocument>.Filter.And(
            Builders<IdeaDocument>.Filter.Eq(i => i.Id, ideaId),
            Builders<IdeaDocument>.Filter.Eq(i => i.TopicId, topicId),
            Builders<IdeaDocument>.Filter.Eq(i => i.DeletedAt, null)
        );

        var update = Builders<IdeaDocument>.Update
            .Set(i => i.DeletedAt, DateTime.UtcNow)
            .Set(i => i.UpdatedAt, DateTime.UtcNow);
        
        var result = await _ideas.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        
        return result.ModifiedCount > 0;
    }
    private static SortDefinition<IdeaDocument> BuildSort(string[]? sort)
    {
        if (sort is null || sort.Length == 0)
            return Builders<IdeaDocument>.Sort.Descending(i => i.CreatedAt);

        var defs = sort.Select(s =>
        {
            var parts = s.Split(',');
            var field = parts[0].Trim();
            var desc  = parts.Length > 1 && parts[1].Trim().ToLower() == "desc";

            return field switch
            {
                "voteCount" => desc
                    ? Builders<IdeaDocument>.Sort.Descending(i => i.VoteCount)
                    : Builders<IdeaDocument>.Sort.Ascending(i => i.VoteCount),
                _ => desc
                    ? Builders<IdeaDocument>.Sort.Descending(i => i.CreatedAt)
                    : Builders<IdeaDocument>.Sort.Ascending(i => i.CreatedAt)
            };
        });

        return Builders<IdeaDocument>.Sort.Combine(defs);
    }

    private static IdeaDetails MapToDomain(IdeaDocument doc) => new()
    {
        Id = doc.Id,
        TopicId = doc.TopicId,
        Title = doc.Title,
        Description = doc.Description,
        AuthorId = doc.AuthorId,
        VoteCount = doc.VoteCount,
        CreatedAt = doc.CreatedAt,
        UpdatedAt = doc.UpdatedAt,
        DeletedAt = doc.DeletedAt
    };
}
