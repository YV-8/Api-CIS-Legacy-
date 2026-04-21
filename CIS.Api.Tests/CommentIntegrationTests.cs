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

    [Fact]
    public async Task CreateAndListComments_WhenCommentsAllowed_ReturnsCreatedAndOk()
    {
        var client = _factory.CreateClient();
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(allowComments: true);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken("commenter-1", "USER"));

        var createResponse = await client.PostAsync(
            $"/api/v1/topics/{topicId}/ideas/{ideaId}/comments",
            Json("""
            {
              "content": "Comentario de prueba"
            }
            """));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var listResponse = await client.GetAsync($"/api/v1/topics/{topicId}/ideas/{ideaId}/comments");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var payload = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("Comentario de prueba", payload);
    }

    [Fact]
    public async Task CreateComment_WhenCommentsDisabled_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var (topicId, ideaId) = await SeedTopicAndIdeaAsync(allowComments: false);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken("commenter-1", "USER"));

        var response = await client.PostAsync(
            $"/api/v1/topics/{topicId}/ideas/{ideaId}/comments",
            Json("""
            {
              "content": "No debería crearse"
            }
            """));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<(string TopicId, string IdeaId)> SeedTopicAndIdeaAsync(bool allowComments)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CisDbContext>();

        var topic = new Topic
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Topic " + Guid.NewGuid().ToString().Substring(0, 8),
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