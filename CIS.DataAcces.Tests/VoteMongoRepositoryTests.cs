using CIS.DataAcces.Data;
using CIS.DataAcces.MongoDB.Documents;
using CIS.DataAcces.MongoDB.Repositories;
using Mongo2Go;
using MongoDB.Driver;
using Xunit;

namespace CIS.DataAcces.Tests;

public class VoteMongoRepositoryTests : IDisposable
{
    private readonly MongoDbRunner _runner;
    private readonly MongoDbContext _context;

    public VoteMongoRepositoryTests()
    {
        _runner  = MongoDbRunner.Start();
        _context = new MongoDbContext(_runner.ConnectionString, "cis_test");
    }

    public void Dispose() => _runner.Dispose();

    private VoteMongoRepository Repo() => new(_context);

    private async Task<Guid> SeedIdea()
    {
        var id  = Guid.NewGuid();
        var col = _context.GetCollection<IdeaDocument>("ideas");
        await col.InsertOneAsync(new IdeaDocument
        {
            Id = id, TopicId = Guid.NewGuid(),
            Title = "Idea", AuthorId = "u1",
            VoteCount = 0,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        return id;
    }

    [Fact]
    public async Task AddVoteAsync_CreatesVoteAndIncrementsCounter()
    {
        var ideaId = await SeedIdea();

        var result = await Repo().AddVoteAsync(ideaId.ToString(), "user-1");

        Assert.Equal(ideaId.ToString(), result.IdeaId);
        Assert.Equal("user-1", result.UserId);

        var col = _context.GetCollection<IdeaDocument>("ideas");
        var doc = await col.Find(i => i.Id == ideaId).FirstOrDefaultAsync();
        Assert.Equal(1, doc.VoteCount);
    }

    [Fact]
    public async Task AddVoteAsync_ThrowsForInvalidGuid()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Repo().AddVoteAsync("no-es-guid", "user-1"));
    }

    [Fact]
    public async Task HasUserVotedAsync_ReturnsFalse_WhenNoVote()
    {
        var ideaId = await SeedIdea();

        var result = await Repo()
            .HasUserVotedAsync(ideaId.ToString(), "user-1");

        Assert.False(result);
    }

    [Fact]
    public async Task HasUserVotedAsync_ReturnsTrue_AfterVoting()
    {
        var ideaId = await SeedIdea();
        await Repo().AddVoteAsync(ideaId.ToString(), "user-1");

        var result = await Repo()
            .HasUserVotedAsync(ideaId.ToString(), "user-1");

        Assert.True(result);
    }

    [Fact]
    public async Task RemoveVoteAsync_DecrementsCounter()
    {
        var ideaId = await SeedIdea();
        await Repo().AddVoteAsync(ideaId.ToString(), "user-1");

        await Repo().RemoveVoteAsync(ideaId.ToString(), "user-1");

        var col = _context.GetCollection<IdeaDocument>("ideas");
        var doc = await col.Find(i => i.Id == ideaId).FirstOrDefaultAsync();
        Assert.Equal(0, doc.VoteCount);
    }

    [Fact]
    public async Task RemoveVoteAsync_DoesNothing_WhenVoteNotFound()
    {
        var ideaId = await SeedIdea();

        // No lanza excepción aunque no exista el voto
        var ex = await Record.ExceptionAsync(() =>
            Repo().RemoveVoteAsync(ideaId.ToString(), "fantasma"));

        Assert.Null(ex);
    }
}