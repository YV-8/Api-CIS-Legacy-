namespace CIS.BusinessLogic.Persistence;

public interface IVoteRepository
{
    Task<VoteCreationResult> AddVoteAsync(string ideaId, string? userId, CancellationToken cancellationToken = default);

    Task RemoveVoteAsync(string ideaId, string userId, CancellationToken cancellationToken = default);

    Task<bool> HasUserVotedAsync(string ideaId, string userId, CancellationToken cancellationToken = default);
}
