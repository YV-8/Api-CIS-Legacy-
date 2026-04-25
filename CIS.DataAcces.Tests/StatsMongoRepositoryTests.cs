using CIS.DataAcces.Data;
using CIS.DataAcces.MongoDB.Documents;
using CIS.DataAcces.MongoDB.Repositories;
using Mongo2Go;
using MongoDB.Driver;
using Xunit;

namespace CIS.DataAcces.Tests;

public class StatsMongoRepositoryTests : IDisposable
{
    private readonly MongoDbRunner _runner;
    private readonly MongoDbContext _context;

    public StatsMongoRepositoryTests()
    {
        _runner  = MongoDbRunner.Start();
        _context = new MongoDbContext(_runner.ConnectionString, "cis_test");
    }

    public void Dispose() => _runner.Dispose();

    private StatsMongoRepository Repo() => new(_context);

    [Fact]
    public async Task GetTopIdeasAsync_ReturnsMostVoted()
    {
        var topicId = Guid.NewGuid();
        var col     = _context.GetCollection<IdeaDocument>("ideas");

        await col.InsertManyAsync(new[]
        {
            new IdeaDocument { Id = Guid.NewGuid(), TopicId = topicId,
                Title = "Popular", AuthorId = "u1", VoteCount = 10,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new IdeaDocument { Id = Guid.NewGuid(), TopicId = topicId,
                Title = "Poco votos", AuthorId = "u1", VoteCount = 1,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
        });

        var result = await Repo().GetTopIdeasAsync(topicId.ToString(), 10);

        Assert.Equal(2, result.Count);
        Assert.Equal("Popular", result[0].Title);
    }

    [Fact]
    public async Task GetTopIdeasAsync_ExcludesDeletedIdeas()
    {
        var topicId = Guid.NewGuid();
        var col     = _context.GetCollection<IdeaDocument>("ideas");

        await col.InsertManyAsync(new[]
        {
            new IdeaDocument { Id = Guid.NewGuid(), TopicId = topicId,
                Title = "Activa", AuthorId = "u1", VoteCount = 5,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new IdeaDocument { Id = Guid.NewGuid(), TopicId = topicId,
                Title = "Borrada", AuthorId = "u1", VoteCount = 99,
                DeletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
        });

        var result = await Repo().GetTopIdeasAsync(topicId.ToString(), 10);

        Assert.Single(result);
        Assert.Equal("Activa", result[0].Title);
    }

    [Fact]
    public async Task GetTopUsersAsync_CountsIdeasAndComments()
    {
        var topicId = Guid.NewGuid();
        var ideaCol = _context.GetCollection<IdeaDocument>("ideas");
        var commCol = _context.GetCollection<CommentDocument>("comments");

        var ideaId = Guid.NewGuid();
        await ideaCol.InsertOneAsync(new IdeaDocument
        {
            Id = ideaId, TopicId = topicId,
            Title = "Idea", AuthorId = "ana",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await commCol.InsertOneAsync(new CommentDocument
        {
            Id = Guid.NewGuid(), IdeaId = ideaId,
            Content = "ok", UserId = "ana",
            CreatedAt = DateTime.UtcNow
        });

        var result = await Repo().GetTopUsersAsync(10);

        Assert.NotEmpty(result);
        var ana = result.FirstOrDefault(u => u.UserId == "ana");
        Assert.NotNull(ana);
        Assert.Equal(2, ana.ActivityCount);
    }

    [Fact]
    public async Task GetTopTopicsByActivityAsync_ReturnsTopicsOrderedByActivity()
    {
        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();

        var topicCol = _context.GetCollection<TopicDocument>("topics");
        await topicCol.InsertManyAsync(new[]
        {
            new TopicDocument { Id = t1, Title = "Topic Activo" },
            new TopicDocument { Id = t2, Title = "Topic Inactivo" }
        });

        var ideaCol = _context.GetCollection<IdeaDocument>("ideas");
        await ideaCol.InsertManyAsync(new[]
        {
            new IdeaDocument { Id = Guid.NewGuid(), TopicId = t1,
                Title = "I1", AuthorId = "u1", VoteCount = 5,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new IdeaDocument { Id = Guid.NewGuid(), TopicId = t1,
                Title = "I2", AuthorId = "u1", VoteCount = 3,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
        });

        var result = await Repo().GetTopTopicsByActivityAsync(10);

        Assert.NotEmpty(result);
        Assert.Equal("Topic Activo", result[0].Title);
    }
}