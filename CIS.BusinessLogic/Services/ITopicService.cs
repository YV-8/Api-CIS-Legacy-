using CIS.BusinessLogic.Domain;
using CIS.BusinessLogic.dtos;
using System.Threading;

namespace CIS.BusinessLogic.Services;

public interface ITopicService
{
    Task<TopicDetails> CreateTopicAsync(CreateTopicRequest request, string authorId, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<TopicResponse>> GetTopicsAsync(int page, int size, string? authorId, DateTime? createdFrom, DateTime? createdTo, string[]? sort, CancellationToken cancellationToken = default);
    Task<TopicDetails?> GetTopicByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<TopicDetails> UpdateTopicAsync(string id, UpdateTopicRequest request, string requesterId, string requesterRole, CancellationToken cancellationToken = default);
    Task DeleteTopicAsync(string id, string requesterId, string requesterRole, CancellationToken cancellationToken = default);
}
