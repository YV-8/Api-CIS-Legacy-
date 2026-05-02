using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CIS.Api.Tests;

public class CommentIntegrationTests : IClassFixture<CisApiFactory>
{
    private const string TestJwtSecret = "your-super-secret-key-minimum-256-bits-long";
    private readonly CisApiFactory _factory;

    public CommentIntegrationTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    // ─── POST /api/v1/topics/{topicId}/ideas/{ideaId}/comments ───────────────

    [Fact]
    public async Task CreateComment_WithMinimumValidContent_ReturnsCreated()
    {
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(allowComments: true);
        var client = AuthorizedClient("commenter-min", "USER");

        var response = await client.PostAsync(
            $"/api/v1/topics/{topicId}/ideas/{ideaId}/comments",
            Json("""{ "content": "x" }"""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateComment_WithMaximumLengthContent_ReturnsCreated()
    {
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(allowComments: true);
        var client = AuthorizedClient("commenter-max", "USER");
        var content = new string('C', 1000);

        var response = await client.PostAsync(
            $"/api/v1/topics/{topicId}/ideas/{ideaId}/comments",
            Json($$"""{ "content": "{{content}}" }"""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateComment_WithNormalContent_ReturnsCreatedAndContainsId()
    {
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(allowComments: true);
        var client = AuthorizedClient("commenter-normal", "USER");

        var response = await client.PostAsync(
            $"/api/v1/topics/{topicId}/ideas/{ideaId}/comments",
            Json("""{ "content": "Comentario de prueba con contenido normal" }"""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("id", body);
    }

    [Fact]
    public async Task CreateComment_WhenCommentsDisabled_ReturnsForbidden()
    {
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(allowComments: false);
        var client = AuthorizedClient("commenter-disabled", "USER");

        var response = await client.PostAsync(
            $"/api/v1/topics/{topicId}/ideas/{ideaId}/comments",
            Json("""{ "content": "No deberia crearse" }"""));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateComment_WithoutToken_ReturnsUnauthorized()
    {
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(allowComments: true);
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/v1/topics/{topicId}/ideas/{ideaId}/comments",
            Json("""{ "content": "Sin autenticacion" }"""));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ─── GET /api/v1/topics/{topicId}/ideas/{ideaId}/comments ────────────────

    [Fact]
    public async Task GetComments_WithMinimumValidInput_ReturnsOk()
    {
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(allowComments: true);
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/topics/{topicId}/ideas/{ideaId}/comments?page=0&size=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetComments_WithMaximumPageSize_ReturnsOk()
    {
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(allowComments: true);
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/topics/{topicId}/ideas/{ideaId}/comments?page=0&size=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetComments_WithNormalInput_ReturnsOkAndPaginatedBody()
    {
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(allowComments: true);
        var client = AuthorizedClient("commenter-list", "USER");

        await client.PostAsync(
            $"/api/v1/topics/{topicId}/ideas/{ideaId}/comments",
            Json("""{ "content": "Comentario para listar" }"""));

        var response = await client.GetAsync(
            $"/api/v1/topics/{topicId}/ideas/{ideaId}/comments?page=0&size=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("content", body);
        Assert.Contains("Comentario para listar", body);
    }

    [Fact]
    public async Task GetComments_WithNegativePage_ReturnsBadRequest()
    {
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(allowComments: true);
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/topics/{topicId}/ideas/{ideaId}/comments?page=-1&size=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetComments_WithSizeExceedingLimit_ReturnsBadRequest()
    {
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(allowComments: true);
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/topics/{topicId}/ideas/{ideaId}/comments?page=0&size=51");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<(string TopicId, string IdeaId)> SeedTopicAndIdeaAsync(bool allowComments)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CisDbContext>();

        var topic = new Topic
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Topic " + Guid.NewGuid().ToString()[..8],
            Description = "seed topic",
            AuthorId = "author-1",
            Type = CIS.BusinessLogic.Domain.TopicType.other,
            Status = CIS.BusinessLogic.Domain.TopicStatus.active,
            AllowComments = allowComments,
            VoteType = "single"
        };

        var idea = new Idea
        {
            Id = Guid.NewGuid().ToString(),
            Title = "seed idea",
            Description = "seed description",
            TopicId = topic.Id,
            AuthorId = "author-1"
        };

        context.Topics.Add(topic);
        context.Ideas.Add(idea);
        await context.SaveChangesAsync();

        return (topic.Id, idea.Id);
    }

    private HttpClient AuthorizedClient(string sub, string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(sub, role));
        return client;
    }

    private static StringContent Json(string payload) =>
        new(payload, Encoding.UTF8, "application/json");

    private static string CreateToken(string sub, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, sub),
                new Claim("role", role)
            ],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
