using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CIS.Api.Tests;

public class VoteIntegrationTests : IClassFixture<CisApiFactory>
{
    private const string TestJwtSecret = "your-super-secret-key-minimum-256-bits-long";
    private readonly CisApiFactory _factory;

    public VoteIntegrationTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddVote_WithoutToken_OnAnonymousTopic_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var ideaId = await SeedIdeaAsync(anonymousVote: true);

        var response = await client.PostAsync($"/api/v1/ideas/{ideaId}/votes", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddVote_WithoutToken_OnNonAnonymousTopic_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var ideaId = await SeedIdeaAsync(anonymousVote: false);

        var response = await client.PostAsync($"/api/v1/ideas/{ideaId}/votes", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddVote_WithToken_OnAnonymousTopic_StoresNullUserId()
    {
        var client = _factory.CreateClient();
        var ideaId = await SeedIdeaAsync(anonymousVote: true);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken("user-voter", "USER"));

        var response = await client.PostAsync($"/api/v1/ideas/{ideaId}/votes", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var vote = context.Votes.Single(v => v.IdeaId == ideaId);
        Assert.Null(vote.UserId);
    }

    private async Task<string> SeedIdeaAsync(bool anonymousVote)
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
            AnonymousVote = anonymousVote,
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

        return idea.Id;
    }

    private static string CreateToken(string sub, string role)
    {
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(TestJwtSecret));
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