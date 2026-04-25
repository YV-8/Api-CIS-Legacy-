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
            Id = Guid.NewGuid(), // O convertir string a Guid si es necesario
            Title = request.Title,
            Description = request.Description,
            CreatedBy = authorId,
            Status = TopicStatus.draft.ToString(),
            Type = TopicType.other.ToString(),
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null
        };

        await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        return MapToDomain(doc);
    }

    // CUMPLE CON: GetPagedAsync
    public async Task<(IReadOnlyList<TopicDetails> Items, int TotalElements)> GetPagedAsync(
        int page, int size, string? authorId, DateTime? createdFrom, DateTime? createdTo, string[]? sort, CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<TopicDocument>.Filter;
        var filter = filterBuilder.Eq(t => t.DeletedAt, null); // Solo activos

        if (!string.IsNullOrEmpty(authorId))
            filter &= filterBuilder.Eq(t => t.CreatedBy, authorId);

        // ... Agregar filtros de fecha (createdFrom/To) aquí ...

        var total = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var docs = await _collection.Find(filter)
            .Skip(page * size)
            .Limit(size)
            .ToListAsync(cancellationToken);

        return (docs.Select(MapToDomain).ToList(), (int)total);
    }

    // CUMPLE CON: FindActiveByIdAsync
    public async Task<TopicDetails?> FindActiveByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var guidId)) return null;
        
        var doc = await _collection
            .Find(t => t.Id == guidId && t.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        return doc is null ? null : MapToDomain(doc);
    }

    // CUMPLE CON: ExistsActiveAsync
    public async Task<bool> ExistsActiveAsync(string topicId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(topicId, out var guidId)) return false;
        return await _collection.Find(t => t.Id == guidId && t.DeletedAt == null).AnyAsync(cancellationToken);
    }

    // CUMPLE CON: TryUpdateAsync
    public async Task<TopicDetails?> TryUpdateAsync(string id, UpdateTopicRequest request, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var guidId)) return null;

        var filter = Builders<TopicDocument>.Filter.Eq(t => t.Id, guidId);
        var update = Builders<TopicDocument>.Update
            .Set(t => t.Title, request.Title)
            .Set(t => t.Description, request.Description)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.FindOneAndUpdateAsync(filter, update, cancellationToken: cancellationToken);
        return result == null ? null : await FindActiveByIdAsync(id, cancellationToken);
    }

    // CUMPLE CON: TrySoftDeleteAsync
    public async Task<bool> TrySoftDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var guidId)) return false;

        var update = Builders<TopicDocument>.Update.Set(t => t.DeletedAt, DateTime.UtcNow);
        var result = await _collection.UpdateOneAsync(t => t.Id == guidId, update, cancellationToken: cancellationToken);
        
        return result.ModifiedCount > 0;
    }

    // ─── Mappers ───────────────────────────────────────
    private static TopicDetails MapToDomain(TopicDocument d) => new()
    {
        Id = d.Id.ToString(),
        Title = d.Title,
        Description = d.Description,
        AuthorId = d.CreatedBy,
        Status = Enum.Parse<TopicStatus>(d.Status),
        Type = Enum.Parse<TopicType>(d.Type),
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt ?? d.CreatedAt,
        DeletedAt = d.DeletedAt
    };

    private static TopicDocument MapToDocument(TopicDetails t) => new()
    {
        Id = Guid.TryParse(t.Id, out var guid) ? guid : Guid.NewGuid(),
        Title = t.Title,
        Description = t.Description,
        Status = t.Status.ToString(),
        Type = t.Type.ToString(),
        CreatedBy = t.AuthorId,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
        DeletedAt = t.DeletedAt
    };
}
