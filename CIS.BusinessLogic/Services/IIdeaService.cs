using CIS.BusinessLogic.dtos;

namespace CIS.BusinessLogic.Services;

public interface IIdeaService
{
    Task<IdeaResponse> CreateIdeaAsync(string topicId, CreateIdeaRequest request, string authorId);
    Task<PaginatedResponse<IdeaResponse>> GetIdeasAsync(string topicId, int page, int size, string? authorId, string[]? sort);
    Task<IdeaResponse> UpdateIdeaAsync(string topicId, string ideaId, UpdateIdeaRequest request, string currentUserId);
    Task DeleteIdeaAsync(string topicId, string ideaId, string currentUserId);
    Task<IEnumerable<TopIdeaResponse>> GetTopIdeasAsync(string? topicId = null, int limit = 200);
    Task<IEnumerable<TopUserResponse>> GetTopUsersAsync(int limit = 10);
}