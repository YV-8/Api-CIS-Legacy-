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

public class TopicIntegrationTests : IClassFixture<CisApiFactory>
{
    private const string TestJwtSecret = "your-super-secret-key-minimum-256-bits-long";
    private readonly CisApiFactory _factory;

    public TopicIntegrationTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    // ─── GET /api/v1/topics ──────────────────────────────────────────────────

    [Fact]
    public async Task GetTopics_WithMinimumValidInput_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/topics?page=0&size=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTopics_WithMaximumPageSize_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/topics?page=0&size=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTopics_WithNormalInput_ReturnsOkAndPaginatedBody()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/topics?page=0&size=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("content", body);
        Assert.Contains("totalElements", body);
    }

    [Fact]
    public async Task GetTopics_WithNegativePage_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/topics?page=-1&size=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTopics_WithZeroSize_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/topics?page=0&size=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTopics_WithSizeExceedingLimit_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/topics?page=0&size=51");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTopics_WithInvalidDateFormat_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/topics?createdFrom=not-a-date");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── POST /api/v1/topics ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateTopic_WithMinimumValidInput_ReturnsCreated()
    {
        var client = AuthorizedClient("user-min", "USER");

        var response = await client.PostAsync("/api/v1/topics", Json("""
            {
              "title": "12345",
              "description": "1234567890"
            }
            """));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateTopic_WithMaximumLengthInput_ReturnsCreated()
    {
        var client = AuthorizedClient("user-max", "USER");
        var title = new string('T', 100);
        var description = new string('D', 1000);

        var response = await client.PostAsync("/api/v1/topics", Json($$"""
            {
              "title": "{{title}}",
              "description": "{{description}}"
            }
            """));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateTopic_WithNormalInput_ReturnsCreatedWithLinks()
    {
        var client = AuthorizedClient("user-normal", "USER");

        var response = await client.PostAsync("/api/v1/topics", Json("""
            {
              "title": "Titulo de prueba normal",
              "description": "Descripcion de prueba con longitud normal"
            }
            """));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("id", body);
        Assert.Contains("links", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateTopic_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/v1/topics", Json("""
            {
              "title": "Titulo de prueba",
              "description": "Descripcion de prueba valida"
            }
            """));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTopic_WithTitleTooShort_ReturnsBadRequest()
    {
        var client = AuthorizedClient("user-short", "USER");

        var response = await client.PostAsync("/api/v1/topics", Json("""
            {
              "title": "abc",
              "description": "Descripcion de prueba valida"
            }
            """));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTopic_WithDescriptionTooShort_ReturnsBadRequest()
    {
        var client = AuthorizedClient("user-desc", "USER");

        var response = await client.PostAsync("/api/v1/topics", Json("""
            {
              "title": "Titulo valido",
              "description": "corto"
            }
            """));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── GET /api/v1/topics/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task GetTopicById_WithExistingId_ReturnsOk()
    {
        var topicId = await SeedTopicAsync("author-read");
        var client = AuthorizedClient("any-user", "USER");

        var response = await client.GetAsync($"/api/v1/topics/{topicId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(topicId, body);
    }

    [Fact]
    public async Task GetTopicById_WithNonExistentId_ReturnsNotFound()
    {
        var client = AuthorizedClient("any-user", "USER");

        var response = await client.GetAsync("/api/v1/topics/non-existent-id-xyz");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTopicById_WithoutToken_ReturnsUnauthorized()
    {
        var topicId = await SeedTopicAsync("author-unauth");
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/topics/{topicId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ─── PUT /api/v1/topics/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateTopic_AsOwner_WithMinimumValidInput_ReturnsOk()
    {
        var authorId = "owner-update-min";
        var topicId = await SeedTopicAsync(authorId);
        var client = AuthorizedClient(authorId, "USER");

        var response = await client.PutAsync($"/api/v1/topics/{topicId}", Json("""
            {
              "title": "12345",
              "description": "1234567890"
            }
            """));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTopic_AsOwner_WithMaximumLengthInput_ReturnsOk()
    {
        var authorId = "owner-update-max";
        var topicId = await SeedTopicAsync(authorId);
        var client = AuthorizedClient(authorId, "USER");
        var title = new string('U', 100);
        var description = new string('D', 1000);

        var response = await client.PutAsync($"/api/v1/topics/{topicId}", Json($$"""
            {
              "title": "{{title}}",
              "description": "{{description}}"
            }
            """));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTopic_AsOwner_WithNormalInput_ReturnsOk()
    {
        var authorId = "owner-update-normal";
        var topicId = await SeedTopicAsync(authorId);
        var client = AuthorizedClient(authorId, "USER");

        var response = await client.PutAsync($"/api/v1/topics/{topicId}", Json("""
            {
              "title": "Titulo actualizado",
              "description": "Descripcion actualizada con contenido normal"
            }
            """));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Titulo actualizado", body);
    }

    [Fact]
    public async Task UpdateTopic_AsNonOwner_ReturnsForbidden()
    {
        var topicId = await SeedTopicAsync("author-original");
        var client = AuthorizedClient("different-user", "USER");

        var response = await client.PutAsync($"/api/v1/topics/{topicId}", Json("""
            {
              "title": "Intento no autorizado",
              "description": "Este usuario no deberia poder editar"
            }
            """));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTopic_WithNonExistentId_ReturnsNotFound()
    {
        var client = AuthorizedClient("some-user", "USER");

        var response = await client.PutAsync("/api/v1/topics/id-que-no-existe", Json("""
            {
              "title": "Titulo cualquiera",
              "description": "Descripcion cualquiera valida"
            }
            """));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─── DELETE /api/v1/topics/{id} ──────────────────────────────────────────

    [Fact]
    public async Task DeleteTopic_AsOwner_ReturnsNoContent()
    {
        var authorId = "owner-delete";
        var topicId = await SeedTopicAsync(authorId);
        var client = AuthorizedClient(authorId, "USER");

        var response = await client.DeleteAsync($"/api/v1/topics/{topicId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTopic_AsAdmin_ReturnsNoContent()
    {
        var topicId = await SeedTopicAsync("topic-author-admin");
        var client = AuthorizedClient("admin-user", "ADMIN");

        var response = await client.DeleteAsync($"/api/v1/topics/{topicId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTopic_AsNonOwner_ReturnsForbidden()
    {
        var topicId = await SeedTopicAsync("real-owner");
        var client = AuthorizedClient("intruder", "USER");

        var response = await client.DeleteAsync($"/api/v1/topics/{topicId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<string> SeedTopicAsync(string authorId, bool allowComments = true)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CisDbContext>();

        var topic = new Topic
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Topic " + Guid.NewGuid().ToString()[..8],
            Description = "Descripcion de topic de prueba",
            AuthorId = authorId,
            Type = CIS.BusinessLogic.Domain.TopicType.other,
            Status = CIS.BusinessLogic.Domain.TopicStatus.active,
            AllowComments = allowComments,
            VoteType = "single"
        };

        context.Topics.Add(topic);
        await context.SaveChangesAsync();

        return topic.Id;
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
