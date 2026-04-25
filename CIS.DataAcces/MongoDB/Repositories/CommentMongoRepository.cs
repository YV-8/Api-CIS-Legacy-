using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Data;
using MongoDB.Driver;

namespace CIS.DataAcces.MongoDB.Repositories;

public class CommentMongoRepository : ICommentRepository
{
    private readonly IMongoCollection<CommentDocument> _comments;

    public CommentMongoRepository(MongoDbContext context)
    {
        _comments = context.GetCollection<CommentDocument>("comments");
    }

    public async Task<CommentDetails> InsertAsync(
        CommentInsertData data,
        CancellationToken cancellationToken = default)
    {
        var doc = new CommentDocument
        {
            Id        = Guid.NewGuid().ToString(),
            IdeaId    = data.IdeaId,
            Content   = data.Content,
            UserId    = data.UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _comments.InsertOneAsync(doc, cancellationToken: cancellationToken);
        return MapToDomain(doc);
    }

    public async Task<(IReadOnlyList<CommentDetails> Items, int TotalElements)> GetPagedForIdeaAsync(
        string ideaId,
        int page,
        int size,
        string[]? sort,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<CommentDocument>.Filter.Eq(c => c.IdeaId, ideaId);

        var total = (int)await _comments
            .CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var items = await _comments
            .Find(filter)
            .Sort(BuildSort(sort))
            .Skip(page * size)
            .Limit(size)
            .ToListAsync(cancellationToken);

        return (items.Select(MapToDomain).ToList(), total);
    }

    private static SortDefinition<CommentDocument> BuildSort(string[]? sort)
    {
        if (sort is null || sort.Length == 0)
            return Builders<CommentDocument>.Sort.Descending(c => c.CreatedAt);

        var defs = sort.Select(s =>
        {
            var parts = s.Split(',');
            var desc  = parts.Length > 1 && parts[1].Trim().ToLower() == "desc";
            return desc
                ? Builders<CommentDocument>.Sort.Descending(c => c.CreatedAt)
                : Builders<CommentDocument>.Sort.Ascending(c => c.CreatedAt);
        });

        return Builders<CommentDocument>.Sort.Combine(defs);
    }

    private static CommentDetails MapToDomain(CommentDocument d) => new()
    {
        Id        = d.Id,
        IdeaId    = d.IdeaId,
        Content   = d.Content,
        UserId    = d.UserId,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt
    };
}