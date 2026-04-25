using CIS.BusinessLogic.dtos;
using CIS.DataAcces.Data;
using CIS.DataAcces.MongoDB.Documents;
using CIS.DataAcces.MongoDB.Repositories;
using CIS.BusinessLogic.Persistence;
using Mongo2Go;
using MongoDB.Driver;
using Xunit;

namespace CIS.DataAcces.Tests;

public class IdeaMongoRepositoryTests : IDisposable
{
    private readonly MongoDbRunner _runner;
    private readonly MongoDbContext _context;

    public IdeaMongoRepositoryTests()
    {
        _runner  = MongoDbRunner.Start();
        _context = new MongoDbContext(_runner.ConnectionString, "cis_test");
    }

    public void Dispose() => _runner.Dispose();

    // ── helpers ────────────────────────────────────────────────
    private IdeaMongoRepository Repo() => new(_context);

    private async Task SeedIdea(IdeaDocument doc)
    {
        var col = _context.GetCollection<IdeaDocument>("ideas");
        await col.InsertOneAsync(doc);
    }

    // ── tests ──────────────────────────────────────────────────
    [Fact]
    public async Task InsertAsync_CreatesIdeaAndReturnsDetails()
    {
        var data = new IdeaInsertData("Mi idea", "Descripción", Guid.NewGuid().ToString(), "user-1");

        var result = await Repo().InsertAsync(data);

        Assert.NotEmpty(result.Id);
        Assert.Equal("Mi idea", result.Title);
        Assert.Equal(0, result.VoteCount);
    }

    [Fact]
    public async Task GetPagedForTopicAsync_ExcludesDeletedIdeas()
    {
        var topicId = Guid.NewGuid();

        await SeedIdea(new IdeaDocument
        {
            Id = Guid.NewGuid(), TopicId = topicId,
            Title = "Activa", AuthorId = "u1",
            DeletedAt = null,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await SeedIdea(new IdeaDocument
        {
            Id = Guid.NewGuid(), TopicId = topicId,
            Title = "Borrada", AuthorId = "u1",
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var (items, total) = await Repo()
            .GetPagedForTopicAsync(topicId.ToString(), 1, 10, null, null);

        Assert.Equal(1, total);
        Assert.Equal("Activa", items[0].Title);
    }

    [Fact]
    public async Task GetPagedForTopicAsync_FiltersByAuthorId()
    {
        var topicId = Guid.NewGuid();

        await SeedIdea(new IdeaDocument
        {
            Id = Guid.NewGuid(), TopicId = topicId,
            Title = "De Ana", AuthorId = "ana",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await SeedIdea(new IdeaDocument
        {
            Id = Guid.NewGuid(), TopicId = topicId,
            Title = "De Bob", AuthorId = "bob",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var (items, total) = await Repo()
            .GetPagedForTopicAsync(topicId.ToString(), 1, 10, "ana", null);

        Assert.Equal(1, total);
        Assert.Equal("De Ana", items[0].Title);
    }

    [Fact]
    public async Task FindInTopicReadAsync_ReturnsNull_IfIdeaIsDeleted()
    {
        var topicId = Guid.NewGuid();
        var ideaId  = Guid.NewGuid();

        await SeedIdea(new IdeaDocument
        {
            Id = ideaId, TopicId = topicId,
            Title = "Borrada", AuthorId = "u1",
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var result = await Repo()
            .FindInTopicReadAsync(topicId.ToString(), ideaId.ToString());

        Assert.Null(result);
    }

    [Fact]
    public async Task FindInTopicReadAsync_ReturnsIdea_WhenExists()
    {
        var topicId = Guid.NewGuid();
        var ideaId  = Guid.NewGuid();

        await SeedIdea(new IdeaDocument
        {
            Id = ideaId, TopicId = topicId,
            Title = "Existe", AuthorId = "u1",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var result = await Repo()
            .FindInTopicReadAsync(topicId.ToString(), ideaId.ToString());

        Assert.NotNull(result);
        Assert.Equal("Existe", result.Title);
    }

    [Fact]
    public async Task TryUpdateAsync_UpdatesTitleAndDescription()
    {
        var topicId = Guid.NewGuid();
        var ideaId  = Guid.NewGuid();

        await SeedIdea(new IdeaDocument
        {
            Id = ideaId, TopicId = topicId,
            Title = "Viejo", Description = "Viejo", AuthorId = "u1",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var request = new UpdateIdeaRequest { Title = "Nuevo", Description = "Nuevo desc" };
        var result  = await Repo()
            .TryUpdateAsync(topicId.ToString(), ideaId.ToString(), request);

        Assert.NotNull(result);
        Assert.Equal("Nuevo", result.Title);
        Assert.Equal("Nuevo desc", result.Description);
    }

    [Fact]
    public async Task TrySoftDeleteAsync_SetsDeletedAt()
    {
        var topicId = Guid.NewGuid();
        var ideaId  = Guid.NewGuid();

        await SeedIdea(new IdeaDocument
        {
            Id = ideaId, TopicId = topicId,
            Title = "A borrar", AuthorId = "u1",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var result = await Repo()
            .TrySoftDeleteAsync(topicId.ToString(), ideaId.ToString());

        Assert.True(result);

        var col = _context.GetCollection<IdeaDocument>("ideas");
        var doc = await col.Find(i => i.Id == ideaId).FirstOrDefaultAsync();
        Assert.NotNull(doc.DeletedAt);
    }

    [Fact]
    public async Task TrySoftDeleteAsync_ReturnsFalse_WhenIdeaNotFound()
    {
        var result = await Repo()
            .TrySoftDeleteAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        Assert.False(result);
    }
}