using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.dtos;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using CIS.DataAcces.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIS.DataAcces.Tests;

public class IdeaRepositoryTests
{
    private static CisDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CisDbContext(options);
    }

    [Fact]
    public async Task GetPagedForTopicAsync_ExcludesDeletedIdeas()
    {
        using var context = CreateInMemoryContext();
        context.Ideas.AddRange(
            new Idea { TopicId = "T1", Title = "Activa", DeletedAt = null, AuthorId = "A1" },
            new Idea { TopicId = "T1", Title = "Borrada", DeletedAt = DateTime.UtcNow, AuthorId = "A1" }
        );
        await context.SaveChangesAsync();
        var repo = new IdeaRepository(context);

        var (items, total) = await repo.GetPagedForTopicAsync("T1", 0, 10, null, null);

        Assert.Equal(1, total);
        Assert.Equal("Activa", items[0].Title);
    }

    [Fact]
    public async Task TryUpdateAsync_WhenIdeaExists_UpdatesTitleAndDescription()
    {
        using var context = CreateInMemoryContext();
        var id = "idea-edit";
        context.Ideas.Add(new Idea { Id = id, TopicId = "T1", Title = "Viejo", Description = "Viejo" });
        await context.SaveChangesAsync();
        var repo = new IdeaRepository(context);

        var request = new UpdateIdeaRequest { Title = "Nuevo", Description = "Nuevo" };
        var result = await repo.TryUpdateAsync("T1", id, request);

        Assert.NotNull(result);
        Assert.Equal("Nuevo", result.Title);
        
        var inDb = await context.Ideas.FindAsync(id);
        Assert.Equal("Nuevo", inDb!.Title);
    }

    [Fact]
    public async Task TrySoftDeleteAsync_RemovesVotesAndComments_ButOnlyMarksIdeaAsDeleted()
    {

        using var context = CreateInMemoryContext();
        var ideaId = "i1";
        var topicId = "t1";
        
        var idea = new Idea { Id = ideaId, TopicId = topicId, Title = "Test" };
        context.Ideas.Add(idea);
        context.Votes.Add(new Vote { IdeaId = ideaId, UserId = "u1" });
        context.Comments.Add(new Comment { IdeaId = ideaId, Content = "c1", UserId = "u1" });
        await context.SaveChangesAsync();

        var repo = new IdeaRepository(context);


        var result = await repo.TrySoftDeleteAsync(topicId, ideaId);

        Assert.True(result);
        
        var dbIdea = await context.Ideas.FindAsync(ideaId);
        Assert.NotNull(dbIdea!.DeletedAt);

        Assert.Empty(await context.Votes.Where(v => v.IdeaId == ideaId).ToListAsync());
        Assert.Empty(await context.Comments.Where(c => c.IdeaId == ideaId).ToListAsync());
    }

    [Fact]
    public async Task FindInTopicReadAsync_ReturnsNull_IfIdeaIsDeleted()
    {

        using var context = CreateInMemoryContext();
        context.Ideas.Add(new Idea { Id = "i1", TopicId = "t1", DeletedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        var repo = new IdeaRepository(context);

        var result = await repo.FindInTopicReadAsync("t1", "i1");

        Assert.Null(result);
    }
}