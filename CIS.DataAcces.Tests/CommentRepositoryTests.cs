using CIS.BusinessLogic.Domain;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using CIS.DataAcces.Repositories;
using CIS.BusinessLogic.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIS.DataAcces.Tests;

public class CommentRepositoryTests
{
    private static CisDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CisDbContext(options);
    }

    [Fact]
    public async Task InsertAsync_AddsCommentToDatabase()
    {

        using var context = CreateInMemoryContext();
        var repo = new CommentRepository(context);
        var data = new CommentInsertData("Nuevo comentario", "idea-123", "user-456");

        var result = await repo.InsertAsync(data);

        var commentInDb = await context.Comments.FirstOrDefaultAsync();
        Assert.NotNull(commentInDb);
        Assert.Equal("Nuevo comentario", commentInDb.Content);
        Assert.Equal(result.Id, commentInDb.Id);
    }

    [Fact]
    public async Task GetPagedForIdeaAsync_FiltersByIdeaId_Correctly()
    {
        using var context = CreateInMemoryContext();
        context.Comments.AddRange(
            new Comment { IdeaId = "idea-1", Content = "C1", UserId = "u1" },
            new Comment { IdeaId = "idea-1", Content = "C2", UserId = "u2" },
            new Comment { IdeaId = "idea-2", Content = "C3", UserId = "u3" }
        );
        await context.SaveChangesAsync();
        var repo = new CommentRepository(context);

        var (items, total) = await repo.GetPagedForIdeaAsync("idea-1", 0, 10, null);

        Assert.Equal(2, total);
        Assert.All(items, c => Assert.Equal("idea-1", c.IdeaId));
    }
}