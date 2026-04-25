using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Data;
using CIS.DataAcces.MongoDB.Documents;
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
            Id        = Guid.NewGuid(),
            IdeaId    = Guid.Parse(data.IdeaId),
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
        if (!Guid.TryParse(ideaId, out var gIdeaId))
            return ([], 0);

        var filter = Builders<CommentDocument>.Filter.Eq(c => c.IdeaId, gIdeaId);

        var totalElements = (int)await _comments
            .CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var sortDef = BuildSort(sort);

        var items = await _comments
            .Find(filter)
            .Sort(sortDef)
            .Skip((page - 1) * size)
            .Limit(size)
            .ToListAsync(cancellationToken);

        return (items.Select(MapToDomain).ToList(), totalElements);
    }

    private static SortDefinition<CommentDocument> BuildSort(string[]? sort)
    {
        if (sort is null || sort.Length == 0)
            return Builders<CommentDocument>.Sort.Descending(c => c.CreatedAt);

        var sortBuilder = Builders<CommentDocument>.Sort;
        var defs = new List<SortDefinition<CommentDocument>>();

        foreach (var s in sort)
        {
            var parts = s.Split(',');
            var field = parts[0].Trim();
            var dir   = parts.Length > 1 ? parts[1].Trim().ToLower() : "asc";

            defs.Add(field switch
            {
                "createdAt" => dir == "desc"
                    ? sortBuilder.Descending(c => c.CreatedAt)
                    : sortBuilder.Ascending(c => c.CreatedAt),
                _ => sortBuilder.Descending(c => c.CreatedAt)
            });
        }

        return sortBuilder.Combine(defs);
    }

    private static CommentDetails MapToDomain(CommentDocument d) => new()
    {
        Id        = d.Id.ToString(),
        IdeaId    = d.IdeaId.ToString(),
        Content   = d.Content,
        UserId    = d.UserId,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt
    };
}