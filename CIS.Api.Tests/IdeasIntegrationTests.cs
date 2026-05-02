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

public class IdeasIntegrationTests : IClassFixture<CisApiFactory>
{
    private const string TestJwtSecret = "your-super-secret-key-minimum-256-bits-long";
    private readonly CisApiFactory _factory;

    public IdeasIntegrationTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    // ─── GET /api/v1/topics/{topicId}/ideas ──────────────────────────────────

    [Fact]
    public async Task GetIdeas_WithMinimumValidInput_ReturnsOk()
    {
        var topicId = await SeedTopicAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/topics/{topicId}/ideas?page=0&size=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetIdeas_WithMaximumPageSize_ReturnsOk()
    {
        var topicId = await SeedTopicAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/topics/{topicId}/ideas?page=0&size=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetIdeas_WithNormalInput_ReturnsOkAndPaginatedBody()
    {
        var topicId = await SeedTopicAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/topics/{topicId}/ideas?page=0&size=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("content", body);
        Assert.Contains("totalElements", body);
    }

    [Fact]
    public async Task GetIdeas_WithNegativePage_ReturnsBadRequest()
    {
        var topicId = await SeedTopicAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/topics/{topicId}/ideas?page=-1&size=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetIdeas_WithSizeExceedingLimit_ReturnsBadRequest()
    {
        var topicId = await SeedTopicAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/topics/{topicId}/ideas?page=0&size=51");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetIdeas_OnNonExistentTopic_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/topics/topic-no-existe/ideas?page=0&size=10");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─── POST /api/v1/topics/{topicId}/ideas ─────────────────────────────────

    [Fact]
    public async Task CreateIdea_WithMinimumValidInput_ReturnsCreated()
    {
        var topicId = await SeedTopicAsync();
        var client = AuthorizedClient("user-min-idea", "USER");

        var response = await client.PostAsync($"/api/v1/topics/{topicId}/ideas", Json("""
            {
              "title": "abc",
              "description": "12345"
            }
            """));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateIdea_WithMaximumLengthInput_ReturnsCreated()
    {
        var topicId = await SeedTopicAsync();
        var client = AuthorizedClient("user-max-idea", "USER");
        var title = new string('I', 200);
        var description = new string('D', 2000);

        var response = await client.PostAsync($"/api/v1/topics/{topicId}/ideas", Json($$"""
            {
              "title": "{{title}}",
              "description": "{{description}}"
            }
            """));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateIdea_WithNormalInput_ReturnsCreatedWithBody()
    {
        var topicId = await SeedTopicAsync();
        var client = AuthorizedClient("user-normal-idea", "USER");

        var response = await client.PostAsync($"/api/v1/topics/{topicId}/ideas", Json("""
            {
              "title": "Idea de prueba normal",
              "description": "Descripcion de la idea con longitud razonable"
            }
            """));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("id", body);
        Assert.Contains("Idea de prueba normal", body);
    }

    [Fact]
    public async Task CreateIdea_WithoutToken_ReturnsUnauthorized()
    {
        var topicId = await SeedTopicAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/topics/{topicId}/ideas", Json("""
            {
              "title": "Idea sin token",
              "description": "No deberia crearse sin autenticacion"
            }
            """));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateIdea_OnNonExistentTopic_ReturnsNotFound()
    {
        var client = AuthorizedClient("user-bad-topic", "USER");

        var response = await client.PostAsync("/api/v1/topics/topic-fantasma/ideas", Json("""
            {
              "title": "Idea huerfana",
              "description": "Topic no existe en la base de datos"
            }
            """));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateIdea_WithTitleTooShort_ReturnsBadRequest()
    {
        var topicId = await SeedTopicAsync();
        var client = AuthorizedClient("user-title-short", "USER");

        var response = await client.PostAsync($"/api/v1/topics/{topicId}/ideas", Json("""
            {
              "title": "ab",
              "description": "Descripcion valida para la idea"
            }
            """));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── PUT /api/v1/topics/{topicId}/ideas/{ideaId} ─────────────────────────

    [Fact]
    public async Task UpdateIdea_AsOwner_WithMinimumValidInput_ReturnsOk()
    {
        var authorId = "owner-idea-min";
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(authorId);
        var client = AuthorizedClient(authorId, "USER");

        var response = await client.PutAsync($"/api/v1/topics/{topicId}/ideas/{ideaId}", Json("""
            {
              "title": "abc",
              "description": "12345"
            }
            """));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateIdea_AsOwner_WithMaximumLengthInput_ReturnsOk()
    {
        var authorId = "owner-idea-max";
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(authorId);
        var client = AuthorizedClient(authorId, "USER");
        var title = new string('U', 200);
        var description = new string('D', 2000);

        var response = await client.PutAsync($"/api/v1/topics/{topicId}/ideas/{ideaId}", Json($$"""
            {
              "title": "{{title}}",
              "description": "{{description}}"
            }
            """));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateIdea_AsOwner_WithNormalInput_ReturnsOkWithUpdatedData()
    {
        var authorId = "owner-idea-normal";
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(authorId);
        var client = AuthorizedClient(authorId, "USER");

        var response = await client.PutAsync($"/api/v1/topics/{topicId}/ideas/{ideaId}", Json("""
            {
              "title": "Idea actualizada",
              "description": "Descripcion actualizada con informacion relevante"
            }
            """));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Idea actualizada", body);
    }

    [Fact]
    public async Task UpdateIdea_AsNonOwner_ReturnsForbidden()
    {
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync("original-author");
        var client = AuthorizedClient("intruder-user", "USER");

        var response = await client.PutAsync($"/api/v1/topics/{topicId}/ideas/{ideaId}", Json("""
            {
              "title": "Intento prohibido",
              "description": "Este usuario no deberia poder editar"
            }
            """));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ─── DELETE /api/v1/topics/{topicId}/ideas/{ideaId} ──────────────────────

    [Fact]
    public async Task DeleteIdea_AsOwner_ReturnsOk()
    {
        var authorId = "owner-delete-idea";
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(authorId);
        var client = AuthorizedClient(authorId, "USER");

        var response = await client.DeleteAsync($"/api/v1/topics/{topicId}/ideas/{ideaId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteIdea_AsAdmin_ReturnsOk()
    {
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync("topic-author-idea");
        var client = AuthorizedClient("admin-user-idea", "ADMIN");

        var response = await client.DeleteAsync($"/api/v1/topics/{topicId}/ideas/{ideaId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteIdea_AsNonOwner_ReturnsForbidden()
    {
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync("real-idea-owner");
        var client = AuthorizedClient("random-user-idea", "USER");

        var response = await client.DeleteAsync($"/api/v1/topics/{topicId}/ideas/{ideaId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<string> SeedTopicAsync(string authorId = "seed-author")
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CisDbContext>();

        var topic = new Topic
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Topic " + Guid.NewGuid().ToString()[..8],
            Description = "Descripcion de topic semilla",
            AuthorId = authorId,
            Type = CIS.BusinessLogic.Domain.TopicType.other,
            Status = CIS.BusinessLogic.Domain.TopicStatus.active,
            AllowComments = true,
            VoteType = "single"
        };

        context.Topics.Add(topic);
        await context.SaveChangesAsync();

        return topic.Id;
    }

    private async Task<(string TopicId, string IdeaId)> SeedTopicAndIdeaAsync(string authorId = "seed-author")
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CisDbContext>();

        var topic = new Topic
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Topic " + Guid.NewGuid().ToString()[..8],
            Description = "Descripcion de topic semilla",
            AuthorId = authorId,
            Type = CIS.BusinessLogic.Domain.TopicType.other,
            Status = CIS.BusinessLogic.Domain.TopicStatus.active,
            AllowComments = true,
            VoteType = "single"
        };

        var idea = new Idea
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Idea semilla",
            Description = "Descripcion semilla de la idea",
            TopicId = topic.Id,
            AuthorId = authorId
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
