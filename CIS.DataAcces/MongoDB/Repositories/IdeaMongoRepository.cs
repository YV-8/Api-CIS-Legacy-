using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Data;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace CIS.DataAcces.MongoDB.Repositories;

public class IdeaMongoRepository : IIdeaRepository
{
    private readonly IMongoCollection<IdeaDocument> _context;

    public IdeaMongoRepository(MongoDbContext context)
    {
        _context = context.GetCollection<IdeaDocument>("ideas");
    }

    public async Task<IdeaDetails> InsertAsync(IdeaInsertData d, CancellationToken cancellationToken = default)
    {
        var ideaDoc = new IdeaDocument
        {
            Id = Guid.NewGuid(),
            Title = d.Title,
            Description = d.Description,
            TopicId = Guid.TryParse(d.TopicId, out var tId) ? tId : Guid.Empty,
            AuthorId = d.AuthorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = null
        };

        await _context.InsertOneAsync(ideaDoc, cancellationToken: cancellationToken);
        return MapToDetails(ideaDoc);
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
        var filter = filterBuilder.Eq(i => i.TopicId, gTopicId) & 
                     filterBuilder.Eq(i => i.DeletedAt, null);

        if (!string.IsNullOrWhiteSpace(authorId))
            filter &= filterBuilder.Eq(i => i.AuthorId, authorId);

        var totalElements = await _context.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var docs = await _context.Find(filter)
            .SortByDescending(i => i.CreatedAt)
            .Skip(page * size)
            .Limit(size)
            .ToListAsync(cancellationToken);

        return (docs.Select(MapToDetails).ToList(), (int)totalElements);
    }

    public async Task<IdeaDetails?> FindInTopicReadAsync(string topicId, string ideaId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(ideaId, out var gIdeaId) || !Guid.TryParse(topicId, out var gTopicId))
            return null;

        var filter = Builders<IdeaDocument>.Filter.And(
            Builders<IdeaDocument>.Filter.Eq(i => i.Id, gIdeaId),
            Builders<IdeaDocument>.Filter.Eq(i => i.TopicId, gTopicId),
            Builders<IdeaDocument>.Filter.Eq(i => i.DeletedAt, null)
        );

        var doc = await _context.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return doc == null ? null : MapToDetails(doc);
    }

    public async Task<IdeaDetails?> TryUpdateAsync(string topicId, string ideaId, UpdateIdeaRequest request, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(ideaId, out var gIdeaId) || !Guid.TryParse(topicId, out var gTopicId))
            return null;

        var filter = Builders<IdeaDocument>.Filter.And(
            Builders<IdeaDocument>.Filter.Eq(i => i.Id, gIdeaId),
            Builders<IdeaDocument>.Filter.Eq(i => i.TopicId, gTopicId),
            Builders<IdeaDocument>.Filter.Eq(i => i.DeletedAt, null)
        );

        var update = Builders<IdeaDocument>.Update
            .Set(i => i.Title, request.Title)
            .Set(i => i.Description, request.Description)
            .Set(i => i.UpdatedAt, DateTime.UtcNow);

        var result = await _context.FindOneAndUpdateAsync(
            filter, 
            update, 
            new FindOneAndUpdateOptions<IdeaDocument> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
        
        return result == null ? null : MapToDetails(result);
    }

    public async Task<bool> TrySoftDeleteAsync(string topicId, string ideaId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(ideaId, out var gIdeaId) || !Guid.TryParse(topicId, out var gTopicId))
            return false;

        var filter = Builders<IdeaDocument>.Filter.And(
            Builders<IdeaDocument>.Filter.Eq(i => i.Id, gIdeaId),
            Builders<IdeaDocument>.Filter.Eq(i => i.TopicId, gTopicId),
            Builders<IdeaDocument>.Filter.Eq(i => i.DeletedAt, null)
        );

        var update = Builders<IdeaDocument>.Update
            .Set(i => i.DeletedAt, DateTime.UtcNow)
            .Set(i => i.UpdatedAt, DateTime.UtcNow);
        
        var result = await _context.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        
        return result.ModifiedCount > 0;
    }

    private static IdeaDetails MapToDetails(IdeaDocument doc) => new()
    {
        Id = doc.Id.ToString(),
        TopicId = doc.TopicId.ToString(),
        Title = doc.Title,
        Description = doc.Description,
        AuthorId = doc.AuthorId,
        VoteCount = doc.VoteCount,
        CreatedAt = doc.CreatedAt,
        UpdatedAt = doc.UpdatedAt,
        DeletedAt = doc.DeletedAt
    };
}
