using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Exceptions;
using CIS.BusinessLogic.Persistence;
using CIS.BusinessLogic.Services;
using Moq;
using Xunit;

namespace CIS.BusinessLogic.Tests;

public class TopicServiceTests
{
    private readonly Mock<ITopicRepository> _repositoryMock;
    private readonly TopicService _service;

    public TopicServiceTests()
    {
        _repositoryMock = new Mock<ITopicRepository>();
        _service = new TopicService(_repositoryMock.Object);
    }

    [Fact]
    public async Task CreateTopicAsync_WithValidData_ReturnsTopicDetails()
    {
        var request = new CreateTopicRequest { Title = "New Topic", Description = "Desc" };
        var authorId = "author-1";
        var expectedResponse = new TopicDetails { Id = "guid-1", Title = "New Topic", AuthorId = authorId };

        _repositoryMock.Setup(r => r.InsertAsync(request, authorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _service.CreateTopicAsync(request, authorId);

        Assert.Equal(expectedResponse.Id, result.Id);
        _repositoryMock.Verify(r => r.InsertAsync(request, authorId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTopicAsync_WhenAuthorIdIsEmpty_ThrowsArgumentException()
    {
        var request = new CreateTopicRequest { Title = "Title" };
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateTopicAsync(request, ""));
    }

    [Fact]
    public async Task GetTopicsAsync_ReturnsPaginatedResponse()
    {
        var topics = new List<TopicDetails> 
        { 
            new() { Id = "1", Title = "T1", AuthorId = "auth" } 
        };
        
        _repositoryMock.Setup(r => r.GetPagedAsync(0, 10, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((topics, 1));

        var result = await _service.GetTopicsAsync(0, 10, null, null, null, null);

        Assert.Single(result.Content);
        Assert.Equal(1, result.TotalElements);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetTopicsAsync_InvalidSize_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            _service.GetTopicsAsync(0, 0, null, null, null, null));
    }

    [Fact]
    public async Task GetTopicByIdAsync_WhenExists_ReturnsTopic()
    {
        var topicId = "id-123";
        _repositoryMock.Setup(r => r.FindActiveByIdAsync(topicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TopicDetails { Id = topicId });

        var result = await _service.GetTopicByIdAsync(topicId);

        Assert.NotNull(result);
        Assert.Equal(topicId, result!.Id);
    }

    [Fact]
    public async Task UpdateTopicAsync_ByOwner_UpdatesSuccessfully()
    {
        var id = "topic-1";
        var userId = "user-1";
        var request = new UpdateTopicRequest { Title = "Updated" };
        var existingTopic = new TopicDetails { Id = id, AuthorId = userId };

        _repositoryMock.Setup(r => r.FindActiveByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTopic);
        _repositoryMock.Setup(r => r.TryUpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TopicDetails { Id = id, Title = "Updated" });

        var result = await _service.UpdateTopicAsync(id, request, userId, "USER");

        Assert.Equal("Updated", result.Title);
    }

    [Fact]
    public async Task UpdateTopicAsync_ByNonOwner_ThrowsForbiddenException()
    {

        var id = "topic-1";
        var existingTopic = new TopicDetails { Id = id, AuthorId = "owner-id" };

        _repositoryMock.Setup(r => r.FindActiveByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTopic);

        await Assert.ThrowsAsync<ForbiddenException>(() => 
            _service.UpdateTopicAsync(id, new UpdateTopicRequest(), "other-user", "USER"));
    }

    [Fact]
    public async Task UpdateTopicAsync_ByAdmin_AllowsUpdate()
    {
        var id = "topic-1";
        var existingTopic = new TopicDetails { Id = id, AuthorId = "owner-id" };

        _repositoryMock.Setup(r => r.FindActiveByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTopic);
        _repositoryMock.Setup(r => r.TryUpdateAsync(id, It.IsAny<UpdateTopicRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TopicDetails { Id = id });

        var result = await _service.UpdateTopicAsync(id, new UpdateTopicRequest(), "admin-id", "ADMIN");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DeleteTopicAsync_WhenSuccessful_DoesNotThrow()
    {

        var id = "topic-1";
        var userId = "user-1";
        var existingTopic = new TopicDetails { Id = id, AuthorId = userId };

        _repositoryMock.Setup(r => r.FindActiveByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTopic);
        _repositoryMock.Setup(r => r.TrySoftDeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        await _service.DeleteTopicAsync(id, userId, "USER");
        _repositoryMock.Verify(r => r.TrySoftDeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTopicAsync_NotFound_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.FindActiveByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TopicDetails?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => 
            _service.DeleteTopicAsync("none", "user", "USER"));
    }
}