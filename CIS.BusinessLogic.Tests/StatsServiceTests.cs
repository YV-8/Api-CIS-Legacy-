using CIS.BusinessLogic.Services;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using Microsoft.EntityFrameworkCore;

namespace CIS.BusinessLogic.Tests;

public class StatsServiceTests
{
    private static CisDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CisDbContext(options);
    }

    [Fact]
    public async Task GetTopTopicsByActivityAsync_ReturnsTopicsSortedByIdeaCountDescending()
    {
        await using var context = CreateInMemoryContext();

        var topicWithOneIdea = new Topic
        {
            Title = "Topic 1",
            Description = "desc",
            AuthorId = "author-1",
            Type = TopicType.other,
            Status = TopicStatus.active
        };

        var topicWithThreeIdeas = new Topic
        {
            Title = "Topic 3",
            Description = "desc",
            AuthorId = "author-2",
            Type = TopicType.other,
            Status = TopicStatus.active
        };

        var topicWithTwoIdeas = new Topic
        {
            Title = "Topic 2",
            Description = "desc",
            AuthorId = "author-3",
            Type = TopicType.other,
            Status = TopicStatus.active
        };

        context.Topics.AddRange(topicWithOneIdea, topicWithThreeIdeas, topicWithTwoIdeas);
        context.Ideas.AddRange(
            new Idea { Title = "i1", Description = "desc", TopicId = topicWithOneIdea.Id, AuthorId = "u1", VoteCount = 4 },
            new Idea { Title = "i2", Description = "desc", TopicId = topicWithThreeIdeas.Id, AuthorId = "u2", VoteCount = 1 },
            new Idea { Title = "i3", Description = "desc", TopicId = topicWithThreeIdeas.Id, AuthorId = "u3", VoteCount = 2 },
            new Idea { Title = "i4", Description = "desc", TopicId = topicWithThreeIdeas.Id, AuthorId = "u4", VoteCount = 3 },
            new Idea { Title = "i5", Description = "desc", TopicId = topicWithTwoIdeas.Id, AuthorId = "u5", VoteCount = 5 },
            new Idea { Title = "i6", Description = "desc", TopicId = topicWithTwoIdeas.Id, AuthorId = "u6", VoteCount = 1 }
        );
        await context.SaveChangesAsync();

        var service = new StatsService(context);

        var result = await service.GetTopTopicsByActivityAsync();

        var items = result.ToArray();
        Assert.Equal(3, items.Length);
        Assert.Equal(topicWithThreeIdeas.Id, items[0].TopicId);
        Assert.Equal(3, items[0].IdeaCount);
        Assert.Equal(6, items[0].TotalVotes);
        Assert.Equal(topicWithTwoIdeas.Id, items[1].TopicId);
        Assert.Equal(2, items[1].IdeaCount);
        Assert.Equal(topicWithOneIdea.Id, items[2].TopicId);
        Assert.Equal(1, items[2].IdeaCount);
    }

    [Fact]
    public async Task GetTopTopicsByActivityAsync_WithLimit_ReturnsLimitedResults()
    {
        await using var context = CreateInMemoryContext();

        var topicA = new Topic
        {
            Title = "A",
            Description = "desc",
            AuthorId = "author-a",
            Type = TopicType.other,
            Status = TopicStatus.active
        };
        var topicB = new Topic
        {
            Title = "B",
            Description = "desc",
            AuthorId = "author-b",
            Type = TopicType.other,
            Status = TopicStatus.active
        };

        context.Topics.AddRange(topicA, topicB);
        context.Ideas.AddRange(
            new Idea { Title = "ia1", Description = "desc", TopicId = topicA.Id, AuthorId = "u1", VoteCount = 0 },
            new Idea { Title = "ia2", Description = "desc", TopicId = topicA.Id, AuthorId = "u2", VoteCount = 0 },
            new Idea { Title = "ib1", Description = "desc", TopicId = topicB.Id, AuthorId = "u3", VoteCount = 0 }
        );
        await context.SaveChangesAsync();

        var service = new StatsService(context);

        var result = await service.GetTopTopicsByActivityAsync(limit: 1);

        var items = result.ToArray();
        Assert.Single(items);
        Assert.Equal(topicA.Id, items[0].TopicId);
        Assert.Equal(2, items[0].IdeaCount);
    }
}
