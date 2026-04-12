using CIS.BusinessLogic.dtos;

namespace CIS.BusinessLogic.Services;

public interface IVoteService
{
    Task<VoteResponse> AddVoteAsync(string ideaId, string userId);
    Task RemoveVoteAsync(string ideaId, string userId);
}
