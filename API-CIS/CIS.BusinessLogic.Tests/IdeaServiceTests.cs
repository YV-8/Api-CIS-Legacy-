using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.Exceptions;
using CIS.BusinessLogic.Services;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using CIS.DataAcces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CIS.BusinessLogic.Tests;

public class IdeaServiceTests
{
    private static CisDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CisDbContext(options);
    }

    [Fact]
    public async Task DeleteIdeaAsync_PerformsSoftDeleteAndRemovesVotesAndComments()
    {
        await using var context = CreateInMemoryContext();

        var topic = new Topic
        {
            Title = "Topic 1",
            Description = "desc",
            AuthorId = "author-1",
            Type = TopicType.other,
            Status = TopicStatus.active
        };

        var idea = new Idea
        {
            Title = "Idea 1",
            Description = "desc",
            TopicId = topic.Id,
            AuthorId = "author-1",
            VoteCount = 1
        };

        var vote = new Vote
        {
            IdeaId = idea.Id,
            UserId = "user-1",
            CreatedAt = DateTime.UtcNow
        };

        var comment = new Comment
        {
            IdeaId = idea.Id,
            UserId = "user-2",
            Content = "comment",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Topics.Add(topic);
        context.Ideas.Add(idea);
        context.Votes.Add(vote);
        context.Comments.Add(comment);
        await context.SaveChangesAsync();

        var service = new IdeaService(new IdeaRepository(context), new TopicRepository(context), new VoteRepository(context));

        await service.DeleteIdeaAsync(topic.Id, idea.Id, "author-1", "USER");

        var storedIdea = await context.Ideas.FindAsync(idea.Id);
        var voteCount = await context.Votes.CountAsync(v => v.IdeaId == idea.Id);
        var commentCount = await context.Comments.CountAsync(c => c.IdeaId == idea.Id);

        Assert.NotNull(storedIdea);
        Assert.NotNull(storedIdea!.DeletedAt);
        Assert.Equal(0, voteCount);
        Assert.Equal(0, commentCount);
    }

    [Fact]
    public async Task GetIdeasAsync_ExcludesSoftDeletedIdeas()
    {
        await using var context = CreateInMemoryContext();

        var topic = new Topic
        {
            Title = "Topic 1",
            Description = "desc",
            AuthorId = "author-1",
            Type = TopicType.other,
            Status = TopicStatus.active
        };

        context.Topics.Add(topic);
        context.Ideas.AddRange(
            new Idea
            {
                Title = "Visible",
                Description = "desc",
                TopicId = topic.Id,
                AuthorId = "author-1",
                VoteCount = 0
            },
            new Idea
            {
                Title = "Deleted",
                Description = "desc",
                TopicId = topic.Id,
                AuthorId = "author-2",
                VoteCount = 0,
                DeletedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var service = new IdeaService(new IdeaRepository(context), new TopicRepository(context), new VoteRepository(context));

        var result = await service.GetIdeasAsync(topic.Id, 0, 10, null, null);

        var items = result.Content.ToArray();
        Assert.Single(items);
        Assert.Equal("Visible", items[0].Title);
    }
}
