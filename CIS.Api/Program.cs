using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using CIS.Api.ExceptionHandling;
using CIS.DataAcces;
using CIS.DataAcces.Data;
using CIS.BusinessLogic.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CIS API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var jwtSection = builder.Configuration.GetSection("Auth:Jwt");
var jwtSecret = jwtSection["Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException("Missing required configuration: Auth:Jwt:Secret");
}

var jwtRequireIssuer = jwtSection.GetValue<bool>("RequireIssuer");
var jwtRequireAudience = jwtSection.GetValue<bool>("RequireAudience");
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];
var jwtClockSkewSeconds = jwtSection.GetValue("ClockSkewSeconds", 60);
var signingKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = jwtRequireIssuer,
        ValidateAudience = jwtRequireAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        NameClaimType = JwtRegisteredClaimNames.Sub,
        RoleClaimType = "role",
        ClockSkew = TimeSpan.FromSeconds(jwtClockSkewSeconds),
        IssuerSigningKey = signingKey
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var sub = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var role = context.Principal?.FindFirst("role")?.Value;

            if (string.IsNullOrWhiteSpace(sub) || string.IsNullOrWhiteSpace(role))
            {
                context.Fail("JWT is missing required claims: sub and role.");
            }

            return Task.CompletedTask;
        }
    };
});

var useInMemoryForTests = builder.Configuration.GetValue<bool>("Testing:UseInMemoryDatabase");
if (useInMemoryForTests)
{
    builder.Services.AddDbContext<CisDbContext>(options =>
        options.UseInMemoryDatabase("cis_api_tests_shared"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<CisDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
}

builder.Services.AddCisPersistence(builder.Configuration);

builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<IIdeaService, IdeaService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IVoteService, VoteService>();
builder.Services.AddScoped<IStatsService, StatsService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<CisDbContext>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy"
        });

        await context.Response.WriteAsync(result);
    }
});

app.Run();

public partial class Program { }