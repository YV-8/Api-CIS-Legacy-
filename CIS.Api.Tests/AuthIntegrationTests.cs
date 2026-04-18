using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CIS.Api.Tests;

public class AuthIntegrationTests : IClassFixture<CisApiFactory>
{
    /// <summary>Debe coincidir con <c>Auth:Jwt:Secret</c> en appsettings de la API en pruebas.</summary>
    private const string TestJwtSecret = "your-super-secret-key-minimum-256-bits-long";

    private readonly CisApiFactory _factory;

    public AuthIntegrationTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTokenMissingSub_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(claims: [new Claim("role", "USER")]));

        var response = await client.PostAsync("/api/v1/topics", CreateTopicBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTokenMissingRole_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(claims: [new Claim(JwtRegisteredClaimNames.Sub, "user-test-1")]));

        var response = await client.PostAsync("/api/v1/topics", CreateTopicBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidSubAndRole_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, "user-test-1"),
                new Claim("role", "USER")
            ]));

        var response = await client.PostAsync("/api/v1/topics", CreateTopicBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static StringContent CreateTopicBody() => new(
        """
        {
          "title":"Titulo para test auth",
          "description":"Descripcion para test auth en endpoint protegido"
        }
        """,
        System.Text.Encoding.UTF8,
        "application/json");

    private static string CreateToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(TestJwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
