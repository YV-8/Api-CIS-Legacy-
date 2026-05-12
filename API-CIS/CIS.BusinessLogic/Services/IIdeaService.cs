using CIS.BusinessLogic.dtos;
using System.Threading;

namespace CIS.BusinessLogic.Services;

public interface IIdeaService
{
    Task<IdeaResponse> CreateIdeaAsync(string topicId, CreateIdeaRequest request, string authorId, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<IdeaResponse>> GetIdeasAsync(string topicId, int page, int size, string? authorId, string[]? sort, CancellationToken cancellationToken = default);
    Task<IdeaResponse> UpdateIdeaAsync(string topicId, string ideaId, UpdateIdeaRequest request, string currentUserId, string currentUserRole, CancellationToken cancellationToken = default);
    Task DeleteIdeaAsync(string topicId, string ideaId, string currentUserId, string currentUserRole, CancellationToken cancellationToken = default);
}
