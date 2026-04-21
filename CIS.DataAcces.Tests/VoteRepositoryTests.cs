using CIS.BusinessLogic.Exceptions;
using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using CIS.DataAcces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace CIS.DataAcces.Tests;

public class VoteRepositoryTests
{
    private static CisDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // Crucial: InMemory no soporta transacciones, así que le decimos que ignore la advertencia
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new CisDbContext(options);
    }

    [Fact]
    public async Task AddVoteAsync_WhenValid_IncrementsVoteCount()
    {
        using var context = CreateContext();
        // Arrange: Crear un Tema que permite votos anónimos y una Idea
        var topic = new Topic { Id = "T1", AnonymousVote = true, AuthorId = "u1" };
        var idea = new Idea { Id = "I1", TopicId = "T1", VoteCount = 0, AuthorId = "u1" };
        context.Topics.Add(topic);
        context.Ideas.Add(idea);
        await context.SaveChangesAsync();

        var repo = new VoteRepository(context);

        // Act
        var result = await repo.AddVoteAsync("I1", null); // Voto anónimo

        // Assert
        Assert.Equal(1, idea.VoteCount);
        Assert.Equal("T1", result.TopicId);
        Assert.Single(context.Votes);
    }

    [Fact]
    public async Task AddVoteAsync_WhenUserAlreadyVoted_ThrowsConflictException()
    {
        using var context = CreateContext();
        // Arrange: Usuario ya votó
        var topic = new Topic { Id = "T1", AnonymousVote = false, AuthorId = "u1" };
        var idea = new Idea { Id = "I1", TopicId = "T1", AuthorId = "u1" };
        var existingVote = new Vote { IdeaId = "I1", UserId = "user-1" };
        
        context.Topics.Add(topic);
        context.Ideas.Add(idea);
        context.Votes.Add(existingVote);
        await context.SaveChangesAsync();

        var repo = new VoteRepository(context);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => 
            repo.AddVoteAsync("I1", "user-1"));
    }

    [Fact]
    public async Task AddVoteAsync_RequiresAuth_WhenTopicDoesNotAllowAnonymous()
    {
        using var context = CreateContext();
        // Arrange: Tema NO permite votos anónimos
        var topic = new Topic { Id = "T1", AnonymousVote = false, AuthorId = "u1" };
        var idea = new Idea { Id = "I1", TopicId = "T1", AuthorId = "u1" };
        context.Topics.Add(topic);
        context.Ideas.Add(idea);
        await context.SaveChangesAsync();

        var repo = new VoteRepository(context);

        // Act & Assert: Intentar votar sin UserId
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => 
            repo.AddVoteAsync("I1", null));
    }

    [Fact]
    public async Task RemoveVoteAsync_DecrementsVoteCount_AndRemovesRecord()
    {
        using var context = CreateContext();
        // Arrange: Idea con 1 voto
        var idea = new Idea { Id = "I1", TopicId = "T1", VoteCount = 1, AuthorId = "u1" };
        var vote = new Vote { IdeaId = "I1", UserId = "user-1" };
        context.Ideas.Add(idea);
        context.Votes.Add(vote);
        await context.SaveChangesAsync();

        var repo = new VoteRepository(context);

        // Act
        await repo.RemoveVoteAsync("I1", "user-1");

        // Assert
        Assert.Equal(0, idea.VoteCount);
        Assert.Empty(context.Votes);
    }

    [Fact]
    public async Task HasUserVotedAsync_ReturnsCorrectStatus()
    {
        using var context = CreateContext();
        context.Votes.Add(new Vote { IdeaId = "I1", UserId = "user-1" });
        await context.SaveChangesAsync();

        var repo = new VoteRepository(context);

        Assert.True(await repo.HasUserVotedAsync("I1", "user-1"));
        Assert.False(await repo.HasUserVotedAsync("I1", "user-2"));
    }
}