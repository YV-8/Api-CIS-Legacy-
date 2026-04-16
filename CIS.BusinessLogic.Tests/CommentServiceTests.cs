using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Exceptions;
using CIS.BusinessLogic.Services;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using CIS.DataAcces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CIS.BusinessLogic.Tests;

public class CommentServiceTests
{
    private static CisDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CisDbContext(options);
    }

    private static async Task<(Topic Topic, Idea Idea)> SeedTopicAndIdeaAsync(
        CisDbContext context,
        bool allowComments = true)
    {
        var topic = new Topic
        {
            Title = "Topic 1",
            Description = "desc",
            AuthorId = "author-1",
            Type = TopicType.other,
            Status = TopicStatus.active,
            AllowComments = allowComments
        };

        var idea = new Idea
        {
            Title = "Idea 1",
            Description = "desc",
            TopicId = topic.Id,
            AuthorId = "author-1"
        };

        context.Topics.Add(topic);
        context.Ideas.Add(idea);
        await context.SaveChangesAsync();

        return (topic, idea);
    }

    [Fact]
    public async Task CreateCommentAsync_WhenCommentsAllowed_CreatesComment()
    {
        await using var context = CreateInMemoryContext();
        var (topic, idea) = await SeedTopicAndIdeaAsync(context);
        var service = new CommentService(new TopicRepository(context), new IdeaRepository(context), new CommentRepository(context));

        var result = await service.CreateCommentAsync(
            topic.Id,
            idea.Id,
            new CreateCommentRequest { Content = "First comment" },
            "user-1");

        var commentInDb = await context.Comments.FirstOrDefaultAsync(c => c.Id == result.Id);

        Assert.NotNull(commentInDb);
        Assert.Equal("First comment", commentInDb!.Content);
        Assert.Equal("user-1", commentInDb.UserId);
    }

    [Fact]
    public async Task CreateCommentAsync_WhenCommentsDisabled_ThrowsForbiddenException()
    {
        await using var context = CreateInMemoryContext();
        var (topic, idea) = await SeedTopicAndIdeaAsync(context, allowComments: false);
        var service = new CommentService(new TopicRepository(context), new IdeaRepository(context), new CommentRepository(context));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CreateCommentAsync(
                topic.Id,
                idea.Id,
                new CreateCommentRequest { Content = "Blocked comment" },
                "user-1"));
    }

    [Fact]
    public async Task GetCommentsAsync_ReturnsPaginatedComments()
    {
        await using var context = CreateInMemoryContext();
        var (topic, idea) = await SeedTopicAndIdeaAsync(context);
        context.Comments.AddRange(
            new Comment { Content = "Comment 1", IdeaId = idea.Id, UserId = "u1", CreatedAt = DateTime.UtcNow.AddMinutes(-1), UpdatedAt = DateTime.UtcNow.AddMinutes(-1) },
            new Comment { Content = "Comment 2", IdeaId = idea.Id, UserId = "u2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new CommentService(new TopicRepository(context), new IdeaRepository(context), new CommentRepository(context));

        var result = await service.GetCommentsAsync(topic.Id, idea.Id, 0, 10, null);

        var items = result.Content.ToArray();
        Assert.Equal(2, items.Length);
        Assert.Equal("Comment 2", items[0].Content);
        Assert.Equal(2, result.TotalElements);
    }
}
