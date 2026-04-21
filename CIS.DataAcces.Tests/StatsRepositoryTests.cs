using CIS.BusinessLogic.dtos;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using CIS.DataAcces.Repositories;
using CIS.BusinessLogic.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIS.DataAcces.Tests;

public class StatsRepositoryTests
{
    private static CisDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CisDbContext(options);
    }

    [Fact]
    public async Task GetTopTopicsByActivityAsync_CalculatesCountsAndVotesCorrectly()
    {
        using var context = CreateContext();

        var topic = new Topic { Id = "T1", Title = "A", AuthorId = "u1" };
        var idea1 = new Idea { Id = "I1", TopicId = "T1", VoteCount = 10, AuthorId = "u1" };
        var idea2 = new Idea { Id = "I2", TopicId = "T1", VoteCount = 5, AuthorId = "u1" };
        
        context.Topics.Add(topic);
        context.Ideas.AddRange(idea1, idea2);
        await context.SaveChangesAsync();

        var repo = new StatsRepository(context);

        var result = await repo.GetTopTopicsByActivityAsync(10);

        Assert.Single(result);
        Assert.Equal(2, result[0].IdeaCount);
        Assert.Equal(15, result[0].TotalVotes);
    }

    [Fact]
    public async Task GetTopUsersAsync_AggregatesIdeasAndVotes()
    {
        using var context = CreateContext();
        context.Ideas.Add(new Idea { AuthorId = "user1", TopicId = "T1" });
        context.Votes.Add(new Vote { UserId = "user1", IdeaId = "I1" });

        context.Ideas.Add(new Idea { AuthorId = "user2", TopicId = "T1" });
        await context.SaveChangesAsync();

        var repo = new StatsRepository(context);

        var result = await repo.GetTopUsersAsync(10);

        Assert.Equal("user1", result[0].UserId);
        Assert.Equal(2, result[0].ActivityCount);
    }
}