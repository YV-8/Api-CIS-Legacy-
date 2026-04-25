using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Data;
using CIS.DataAcces.MongoDB.Documents;
using CIS.DataAcces.MongoDB.Repositories;
using Mongo2Go;
using MongoDB.Driver;
using Xunit;

namespace CIS.DataAcces.Tests;

public class CommentMongoRepositoryTests : IDisposable
{
    private readonly MongoDbRunner _runner;
    private readonly MongoDbContext _context;

    public CommentMongoRepositoryTests()
    {
        _runner  = MongoDbRunner.Start();
        _context = new MongoDbContext(_runner.ConnectionString, "cis_test");
    }

    public void Dispose() => _runner.Dispose();

    private CommentMongoRepository Repo() => new(_context);

    private async Task SeedComment(CommentDocument doc)
    {
        var col = _context.GetCollection<CommentDocument>("comments");
        await col.InsertOneAsync(doc);
    }

    [Fact]
    public async Task InsertAsync_CreatesComment_AndReturnsDetails()
    {
        var ideaId = Guid.NewGuid().ToString();
        var data = new CommentInsertData(ideaId, "Buen punto", "user-1");

        var result = await Repo().InsertAsync(data);

        Assert.NotEmpty(result.Id);
        Assert.Equal("Buen punto", result.Content);
    }

    [Fact]
    public async Task GetPagedForIdeaAsync_ReturnsOnlyCommentsForIdea()
    {
        var ideaId  = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        await SeedComment(new CommentDocument
        {
            Id = Guid.NewGuid(), IdeaId = ideaId,
            Content = "Correcto", UserId = "u1",
            CreatedAt = DateTime.UtcNow
        });
        await SeedComment(new CommentDocument
        {
            Id = Guid.NewGuid(), IdeaId = otherId,
            Content = "Otro", UserId = "u2",
            CreatedAt = DateTime.UtcNow
        });

        var (items, total) = await Repo()
            .GetPagedForIdeaAsync(ideaId.ToString(), 1, 10, null);

        Assert.Equal(1, total);
        Assert.Equal("Correcto", items[0].Content);
    }

    [Fact]
    public async Task GetPagedForIdeaAsync_PaginatesCorrectly()
    {
        var ideaId = Guid.NewGuid();
        var col    = _context.GetCollection<CommentDocument>("comments");

        for (int i = 1; i <= 5; i++)
        {
            await col.InsertOneAsync(new CommentDocument
            {
                Id = Guid.NewGuid(), IdeaId = ideaId,
                Content = $"Comentario {i}", UserId = "u1",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }

        var (items, total) = await Repo()
            .GetPagedForIdeaAsync(ideaId.ToString(), 1, 2, null);

        Assert.Equal(5, total);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetPagedForIdeaAsync_ReturnsEmpty_ForUnknownIdea()
    {
        var (items, total) = await Repo()
            .GetPagedForIdeaAsync(Guid.NewGuid().ToString(), 1, 10, null);

        Assert.Equal(0, total);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetPagedForIdeaAsync_ReturnsZero_ForInvalidGuid()
    {
        var (items, total) = await Repo()
            .GetPagedForIdeaAsync("no-es-un-guid", 1, 10, null);

        Assert.Equal(0, total);
        Assert.Empty(items);
    }
}