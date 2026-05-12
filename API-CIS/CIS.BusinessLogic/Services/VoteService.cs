using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Persistence;
using System.Threading;

namespace CIS.BusinessLogic.Services;

public class VoteService : IVoteService
{
    private readonly IVoteRepository _votes;

    public VoteService(IVoteRepository votes)
    {
        _votes = votes;
    }

    public async Task<VoteResponse> AddVoteAsync(string ideaId, string? userId, CancellationToken cancellationToken = default)
    {
        var result = await _votes.AddVoteAsync(ideaId, userId, cancellationToken);

        return new VoteResponse
        {
            Id = result.VoteId,
            IdeaId = result.IdeaId,
            UserId = result.UserId,
            CreatedAt = result.CreatedAt,
            Links = new object[]
            {
                new { rel = "self",   href = $"/api/v1/ideas/{ideaId}/votes" },
                new { rel = "idea",   href = $"/api/v1/topics/{result.TopicId}/ideas/{ideaId}" },
                new { rel = "unvote", href = $"/api/v1/ideas/{ideaId}/votes" }
            }
        };
    }

    public Task RemoveVoteAsync(string ideaId, string userId, CancellationToken cancellationToken = default) =>
        _votes.RemoveVoteAsync(ideaId, userId, cancellationToken);
}
