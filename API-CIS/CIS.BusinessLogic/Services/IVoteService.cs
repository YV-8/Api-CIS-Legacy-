using CIS.BusinessLogic.dtos;
using System.Threading;

namespace CIS.BusinessLogic.Services;

public interface IVoteService
{
    Task<VoteResponse> AddVoteAsync(string ideaId, string? userId, CancellationToken cancellationToken = default);
    Task RemoveVoteAsync(string ideaId, string userId, CancellationToken cancellationToken = default);
}
