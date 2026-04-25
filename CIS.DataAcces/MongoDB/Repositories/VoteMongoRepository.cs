using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Data;
using CIS.DataAcces.MongoDB.Documents;
using MongoDB.Driver;

namespace CIS.DataAcces.MongoDB.Repositories;

public class VoteMongoRepository : IVoteRepository
{
    private readonly IMongoCollection<VoteDocument> _votes;
    private readonly IMongoCollection<IdeaDocument>     _ideas;

    public VoteMongoRepository(MongoDbContext context)
    {
        _votes = context.GetCollection<VoteDocument>("votes");
        _ideas = context.GetCollection<IdeaDocument>("ideas");
    }

    public async Task<VoteCreationResult> AddVoteAsync(
        string ideaId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(ideaId, out var gIdeaId))
            throw new ArgumentException($"Invalid ideaId: {ideaId}");

        // Verificar que la idea existe
        var idea = await _ideas
            .Find(i => i.Id == gIdeaId && i.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Idea {ideaId} not found");

        // Buscar el topicId desde la idea
        var topicId = idea.TopicId.ToString();

        // Registrar el voto
        var voteDoc = new VoteDocument
        {
            Id        = Guid.NewGuid(),
            IdeaId    = gIdeaId,
            UserId    = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _votes.InsertOneAsync(voteDoc, cancellationToken: cancellationToken);

        // Incrementar contador en la idea
        var update = Builders<IdeaDocument>.Update.Inc(i => i.VoteCount, 1);
        await _ideas.UpdateOneAsync(
            i => i.Id == gIdeaId, update,
            cancellationToken: cancellationToken);

        // Retornar VoteCreationResult con sus propiedades reales
        return new VoteCreationResult
        {
            VoteId    = voteDoc.Id.ToString(),
            IdeaId    = ideaId,
            UserId    = userId,
            TopicId   = topicId,
            CreatedAt = voteDoc.CreatedAt
        };
    }

    public async Task RemoveVoteAsync(           // ← Task, no Task<bool>
        string ideaId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(ideaId, out var gIdeaId))
            throw new ArgumentException($"Invalid ideaId: {ideaId}");

        var filter = Builders<VoteDocument>.Filter.And(
            Builders<VoteDocument>.Filter.Eq(v => v.IdeaId, gIdeaId),
            Builders<VoteDocument>.Filter.Eq(v => v.UserId, userId)
        );

        var result = await _votes.DeleteOneAsync(filter, cancellationToken);

        if (result.DeletedCount > 0)
        {
            var update = Builders<IdeaDocument>.Update.Inc(i => i.VoteCount, -1);
            await _ideas.UpdateOneAsync(
                i => i.Id == gIdeaId, update,
                cancellationToken: cancellationToken);
        }
    }

    public async Task<bool> HasUserVotedAsync(   // ← nombre correcto de la interfaz
        string ideaId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(ideaId, out var gIdeaId)) return false;

        var filter = Builders<VoteDocument>.Filter.And(
            Builders<VoteDocument>.Filter.Eq(v => v.IdeaId, gIdeaId),
            Builders<VoteDocument>.Filter.Eq(v => v.UserId, userId)
        );

        return await _votes.Find(filter).AnyAsync(cancellationToken);
    }
}
