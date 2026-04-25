using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Data;
using CIS.DataAcces.MongoDB.Repositories;
using CIS.DataAcces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CIS.DataAcces;

public static class DependencyInjection
{
    public static IServiceCollection AddCisPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Persistence:Provider"]?.ToLower() ?? "rdbms";

        if (provider == "mongodb")   // ← todo minúsculas
            RegisterMongoDB(services, configuration);
        else
            RegisterRdbms(services, configuration);

        return services;
    }

    private static void RegisterRdbms(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ITopicRepository,   TopicRepository>();
        services.AddScoped<IIdeaRepository,    IdeaRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IVoteRepository,    VoteRepository>();
        services.AddScoped<IStatsRepository,   StatsRepository>();
    }

    private static void RegisterMongoDB(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<MongoDbContext>();

        services.AddScoped<ITopicRepository,   TopicMongoRepository>();
        services.AddScoped<IIdeaRepository,    IdeaMongoRepository>();
        services.AddScoped<ICommentRepository, CommentMongoRepository>();
        services.AddScoped<IVoteRepository,    VoteMongoRepository>();
        services.AddScoped<IStatsRepository,   StatsMongoRepository>();
    }
}