using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Services;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CIS.BusinessLogic.Tests;

public class TopicServiceTests
{
    private static CisDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CisDbContext(options);
    }

    [Fact]
    public async Task CreateTopicAsync_WhenAuthorIdIsEmpty_ThrowsArgumentException()
    {
        await using var context = CreateInMemoryContext();
        var service = new TopicService(context);

        var request = new CreateTopicRequest
        {
            Title = "Título válido",
            Description = "Descripción válida con más de 10 caracteres"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateTopicAsync(request, string.Empty));
    }

    [Fact]
    public async Task GetTopicsAsync_AppliesPaginationFilteringAndSorting()
    {
        await using var context = CreateInMemoryContext();
        context.Topics.AddRange(
            new Topic { Title = "t1", Description = "desc1", AuthorId = "author1", Type = TopicType.other, Status = TopicStatus.active, CreatedAt = new DateTime(2025, 1, 1), UpdatedAt = DateTime.UtcNow },
            new Topic { Title = "t2", Description = "desc2", AuthorId = "author2", Type = TopicType.other, Status = TopicStatus.active, CreatedAt = new DateTime(2025, 2, 1), UpdatedAt = DateTime.UtcNow },
            new Topic { Title = "t3", Description = "desc3", AuthorId = "author1", Type = TopicType.other, Status = TopicStatus.active, CreatedAt = new DateTime(2025, 3, 1), UpdatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new TopicService(context);

        var result = await service.GetTopicsAsync(page: 0, size: 2, authorId: "author1", createdFrom: new DateTime(2025, 1, 1), createdTo: new DateTime(2025, 12, 31), sort: new[] { "createdAt,desc" });

        Assert.Equal(2, result.Content.Count());
        Assert.Equal(2, result.TotalElements);
        Assert.Equal(1, result.TotalPages);

        var topics = result.Content.ToArray();
        Assert.Equal("t3", topics[0].Title);
        Assert.Equal("t1", topics[1].Title);
    }

    [Fact]
    public async Task GetTopicsAsync_PageOutOfRange_ReturnsEmptyContent()
    {
        await using var context = CreateInMemoryContext();
        context.Topics.Add(new Topic { Title = "t1", Description = "desc1", AuthorId = "author1", Type = TopicType.other, Status = TopicStatus.active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new TopicService(context);

        var result = await service.GetTopicsAsync(page: 5, size: 10, authorId: null, createdFrom: null, createdTo: null, sort: null);

        Assert.Empty(result.Content);
        Assert.Equal(1, result.TotalElements);
        Assert.Equal(1, result.TotalPages);
    }
}
