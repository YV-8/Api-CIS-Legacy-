using CIS.BusinessLogic.Exceptions;
using CIS.BusinessLogic.Services;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CIS.BusinessLogic.Tests;

public class VoteServiceTests
{
    private static CisDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new CisDbContext(options);
    }

    private static async Task<Idea> SeedIdeaAsync(CisDbContext context, int voteCount = 0)
    {
        var topic = new Topic
        {
            Title = "Test Topic",
            Description = "Test description",
            AuthorId = "author-1",
            Type = TopicType.other,
            Status = TopicStatus.active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var idea = new Idea
        {
            Title = "Test Idea",
            Description = "Test idea description",
            TopicId = topic.Id,
            AuthorId = "author-1",
            VoteCount = voteCount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Topics.Add(topic);
        context.Ideas.Add(idea);
        await context.SaveChangesAsync();

        return idea;
    }

    // AddVoteAsync tests

    [Fact]
    public async Task AddVoteAsync_WithValidData_CreatesVoteAndIncrementsVoteCount()
    {
        await using var context = CreateInMemoryContext();
        var idea = await SeedIdeaAsync(context);
        var service = new VoteService(context);
        const string userId = "user-1";

        var result = await service.AddVoteAsync(idea.Id, userId);

        var voteInDb = await context.Votes.FirstOrDefaultAsync(v => v.IdeaId == idea.Id && v.UserId == userId);
        var updatedIdea = await context.Ideas.FindAsync(idea.Id);

        Assert.NotNull(voteInDb);
        Assert.Equal(idea.Id, result.IdeaId);
        Assert.Equal(userId, result.UserId);
        Assert.NotEmpty(result.Id);
        Assert.Equal(1, updatedIdea!.VoteCount);
        Assert.Equal(3, result.Links.Length);
        Assert.Contains(result.Links, l => l.ToString()!.Contains("self"));
        Assert.Contains(result.Links, l => l.ToString()!.Contains("idea"));
        Assert.Contains(result.Links, l => l.ToString()!.Contains("unvote"));
    }

    [Fact]
    public async Task AddVoteAsync_WhenIdeaNotFound_ThrowsNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var service = new VoteService(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.AddVoteAsync("non-existent-idea-id", "user-1"));
    }

    [Fact]
    public async Task AddVoteAsync_WhenUserAlreadyVoted_ThrowsConflictException()
    {
        await using var context = CreateInMemoryContext();
        var idea = await SeedIdeaAsync(context);
        context.Votes.Add(new Vote { IdeaId = idea.Id, UserId = "user-1", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.AddVoteAsync(idea.Id, "user-1"));
    }

    // RemoveVoteAsync tests

    [Fact]
    public async Task RemoveVoteAsync_WithExistingVote_RemovesVoteAndDecrementsVoteCount()
    {
        await using var context = CreateInMemoryContext();
        var idea = await SeedIdeaAsync(context, voteCount: 1);
        context.Votes.Add(new Vote { IdeaId = idea.Id, UserId = "user-1", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        await service.RemoveVoteAsync(idea.Id, "user-1");

        var voteInDb = await context.Votes.FirstOrDefaultAsync(v => v.IdeaId == idea.Id && v.UserId == "user-1");
        var updatedIdea = await context.Ideas.FindAsync(idea.Id);

        Assert.Null(voteInDb);
        Assert.Equal(0, updatedIdea!.VoteCount);
    }

    [Fact]
    public async Task RemoveVoteAsync_WhenIdeaNotFound_ThrowsNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var service = new VoteService(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.RemoveVoteAsync("non-existent-idea-id", "user-1"));
    }

    [Fact]
    public async Task RemoveVoteAsync_WhenVoteNotFound_ThrowsNotFoundException()
    {
        await using var context = CreateInMemoryContext();
        var idea = await SeedIdeaAsync(context);
        var service = new VoteService(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.RemoveVoteAsync(idea.Id, "user-1"));
    }
}
