using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.Persistence;
using CIS.BusinessLogic.dtos;
using CIS.DataAcces.Data;
using CIS.DataAcces.MongoDB.Documents;
using MongoDB.Driver;

namespace CIS.DataAcces.MongoDB.Repositories;

public class TopicMongoRepository : ITopicRepository
{
    private readonly IMongoCollection<TopicDocument> _collection;

    public TopicMongoRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<TopicDocument>("topics");
    }

    public async Task<TopicDetails> InsertAsync(CreateTopicRequest request, string authorId, CancellationToken cancellationToken = default)
    {
        var doc = new TopicDocument
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Description = request.Description,
            CreatedBy = authorId,
            Status = TopicStatus.draft.ToString(),
            Type = TopicType.other.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = null
        };

        await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        return MapToDomain(doc);
    }

    public async Task<(IReadOnlyList<TopicDetails> Items, int TotalElements)> GetPagedAsync(
        int page, int size, string? authorId, DateTime? createdFrom, DateTime? createdTo, string[]? sort, CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<TopicDocument>.Filter;
        var filter = filterBuilder.Eq(t => t.DeletedAt, null); // Solo activos

        if (!string.IsNullOrEmpty(authorId))
            filter &= filterBuilder.Eq(t => t.CreatedBy, authorId);
        if (createdFrom.HasValue)
            filter &= filterBuilder.Gte(t => t.CreatedAt, createdFrom.Value);
        if (createdTo.HasValue)
            filter &= filterBuilder.Lte(t => t.CreatedAt, createdTo.Value);

        var total = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var docs = await _collection
            .Find(filter)
            .Sort(BuildSort(sort))
            .Skip(page * size)
            .Limit(size)
            .ToListAsync(cancellationToken);

        return (docs.Select(MapToDomain).ToList(), (int)total);
    }

    public async Task<TopicDetails?> FindActiveByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        // ← fix: string directo, sin Guid.TryParse
        var doc = await _collection
            .Find(t => t.Id == id && t.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        return doc is null ? null : MapToDomain(doc);
    }

    public async Task<bool> ExistsActiveAsync(
        string topicId,
        CancellationToken cancellationToken = default)
    {
        // ← fix: string directo
        return await _collection
            .Find(t => t.Id == topicId && t.DeletedAt == null)
            .AnyAsync(cancellationToken);
    }

    public async Task<TopicDetails?> TryUpdateAsync(
        string id,
        UpdateTopicRequest request,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<TopicDocument>.Filter.And(
            Builders<TopicDocument>.Filter.Eq(t => t.Id, id),
            Builders<TopicDocument>.Filter.Eq(t => t.DeletedAt, null)
        );

        var update = Builders<TopicDocument>.Update
            .Set(t => t.Title, request.Title)
            .Set(t => t.Description, request.Description)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);

        // ← fix: ReturnDocument.After — una sola query, no dos
        var opts = new FindOneAndUpdateOptions<TopicDocument>
        {
            ReturnDocument = ReturnDocument.After
        };

        var doc = await _collection.FindOneAndUpdateAsync(filter, update, opts, cancellationToken);
        return doc is null ? null : MapToDomain(doc);
    }

    public async Task<bool> TrySoftDeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        // ← fix: string directo
        var update = Builders<TopicDocument>.Update
            .Set(t => t.DeletedAt, DateTime.UtcNow)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(
            t => t.Id == id && t.DeletedAt == null,
            update,
            cancellationToken: cancellationToken);

        return result.ModifiedCount > 0;
    }

    private static SortDefinition<TopicDocument> BuildSort(string[]? sort)
    {
        if (sort is null || sort.Length == 0)
            return Builders<TopicDocument>.Sort.Descending(t => t.CreatedAt);

        var defs = sort.Select(s =>
        {
            var parts = s.Split(',');
            var field = parts[0].Trim();
            var desc  = parts.Length > 1 && parts[1].Trim().ToLower() == "desc";

            return field switch
            {
                "title"     => desc
                    ? Builders<TopicDocument>.Sort.Descending(t => t.Title)
                    : Builders<TopicDocument>.Sort.Ascending(t => t.Title),
                "createdAt" => desc
                    ? Builders<TopicDocument>.Sort.Descending(t => t.CreatedAt)
                    : Builders<TopicDocument>.Sort.Ascending(t => t.CreatedAt),
                _ => Builders<TopicDocument>.Sort.Descending(t => t.CreatedAt)
            };
        });

        return Builders<TopicDocument>.Sort.Combine(defs);
    }
    private static TopicDetails MapToDomain(TopicDocument d) => new()
    {
        Id = d.Id,
        Title = d.Title,
        Description = d.Description,
        AuthorId = d.CreatedBy,
        Status = Enum.Parse<TopicStatus>(d.Status),
        Type = Enum.Parse<TopicType>(d.Type),
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt ?? d.CreatedAt,
        DeletedAt = d.DeletedAt
    };
}
