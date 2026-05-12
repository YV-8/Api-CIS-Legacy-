using System.Net;
using System.Text;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIS.Api.Tests;

public class StatsIntegrationTests : IClassFixture<CisApiFactory>
{
    private readonly CisApiFactory _factory;

    public StatsIntegrationTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    // ─── GET /api/v1/stats/top ───────────────────────────────────────────────

    [Fact]
    public async Task GetTopTopics_WithNoLimit_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/stats/top");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTopTopics_WithMinimumLimit_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/stats/top?limit=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTopTopics_WithNormalLimit_ReturnsOkAndList()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/stats/top?limit=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task GetTopTopics_WithZeroLimit_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/stats/top?limit=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTopTopics_WithNegativeLimit_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/stats/top?limit=-1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── GET /api/v1/stats/ideas/top ─────────────────────────────────────────

    [Fact]
    public async Task GetTopIdeas_WithNoLimit_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/stats/ideas/top");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTopIdeas_WithMinimumLimit_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/stats/ideas/top?limit=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTopIdeas_FilteredByTopicId_ReturnsOk()
    {
        var topicId = await SeedTopicAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/stats/ideas/top?topicId={topicId}&limit=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTopIdeas_WithZeroLimit_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/stats/ideas/top?limit=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── GET /api/v1/stats/topics/{topicId}/ideas/top ────────────────────────

    [Fact]
    public async Task GetTopIdeasByTopic_WithNoLimit_ReturnsOk()
    {
        var topicId = await SeedTopicAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/stats/topics/{topicId}/ideas/top");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTopIdeasByTopic_WithMinimumLimit_ReturnsOk()
    {
        var topicId = await SeedTopicAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/stats/topics/{topicId}/ideas/top?limit=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTopIdeasByTopic_WithNormalLimit_ReturnsOk()
    {
        var topicId = await SeedTopicAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/stats/topics/{topicId}/ideas/top?limit=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTopIdeasByTopic_WithZeroLimit_ReturnsBadRequest()
    {
        var topicId = await SeedTopicAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/stats/topics/{topicId}/ideas/top?limit=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── GET /api/v1/stats/users/top ─────────────────────────────────────────

    [Fact]
    public async Task GetTopUsers_WithNoLimit_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/stats/users/top");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTopUsers_WithMinimumLimit_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/stats/users/top?limit=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTopUsers_WithNormalLimit_ReturnsOkAndList()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/stats/users/top?limit=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task GetTopUsers_WithZeroLimit_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/stats/users/top?limit=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<string> SeedTopicAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CisDbContext>();

        var topic = new Topic
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Stats topic " + Guid.NewGuid().ToString()[..8],
            Description = "Topic para pruebas de estadisticas",
            AuthorId = "stats-author",
            Type = CIS.BusinessLogic.Domain.TopicType.other,
            Status = CIS.BusinessLogic.Domain.TopicStatus.active,
            AllowComments = true,
            VoteType = "single"
        };

        context.Topics.Add(topic);
        await context.SaveChangesAsync();

        return topic.Id;
    }
}
