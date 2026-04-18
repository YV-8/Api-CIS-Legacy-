using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CIS.DataAcces;

public static class DependencyInjection
{
    public static IServiceCollection AddCisPersistence(this IServiceCollection services)
    {
        services.AddScoped<ITopicRepository, TopicRepository>();
        services.AddScoped<IIdeaRepository, IdeaRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IVoteRepository, VoteRepository>();
        services.AddScoped<IStatsRepository, StatsRepository>();
        return services;
    }
}
