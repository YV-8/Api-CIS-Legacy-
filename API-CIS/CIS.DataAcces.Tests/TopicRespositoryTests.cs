using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Domain;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using CIS.DataAcces.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIS.DataAcces.Tests;

public class TopicRepositoryTests
{
    private static CisDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CisDbContext(options);
    }

    [Fact]
    public async Task InsertAsync_SetsDefaultValuesCorrectly()
    {
        using var context = CreateContext();
        var repo = new TopicRepository(context);
        var request = new CreateTopicRequest { Title = "New Topic", Description = "Desc" };

        var result = await repo.InsertAsync(request, "author-1");

        Assert.Equal(TopicStatus.draft, result.Status);
        Assert.Equal("single", result.VoteType);
        Assert.Equal("author-1", result.AuthorId);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByDateRange()
    {
        using var context = CreateContext();
        var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newDate = DateTime.UtcNow;

        context.Topics.Add(new Topic { Title = "Old", CreatedAt = oldDate, AuthorId = "u" });
        context.Topics.Add(new Topic { Title = "New", CreatedAt = newDate, AuthorId = "u" });
        await context.SaveChangesAsync();

        var repo = new TopicRepository(context);

        var (items, total) = await repo.GetPagedAsync(0, 10, null, DateTime.UtcNow.AddDays(-1), null, null);

        Assert.Equal(1, total);
        Assert.Equal("New", items[0].Title);
    }

    [Fact]
    public async Task TrySoftDeleteAsync_PerformsCascadingSoftDeleteOnIdeas()
    {
        using var context = CreateContext();
        var topic = new Topic { Id = "T1", AuthorId = "u1" };
        var idea = new Idea { Id = "I1", TopicId = "T1", AuthorId = "u1" };
        var vote = new Vote { IdeaId = "I1", UserId = "u1" };

        context.Topics.Add(topic);
        context.Ideas.Add(idea);
        context.Votes.Add(vote);
        await context.SaveChangesAsync();

        var repo = new TopicRepository(context);

        var result = await repo.TrySoftDeleteAsync("T1");

        Assert.True(result);
        var dbTopic = await context.Topics.FindAsync("T1");
        var dbIdea = await context.Ideas.FindAsync("I1");
        
        Assert.NotNull(dbTopic!.DeletedAt);
        Assert.NotNull(dbIdea!.DeletedAt);
        Assert.Empty(context.Votes.ToList());
    }
}