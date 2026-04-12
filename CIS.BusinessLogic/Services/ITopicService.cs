using CIS.BusinessLogic.dtos;
using CIS.DataAcces.Models;

namespace CIS.BusinessLogic.Services;

public interface ITopicService
{
    Task<Topic> CreateTopicAsync(CreateTopicRequest request, string authorId);
    Task<PaginatedResponse<TopicResponse>> GetTopicsAsync(int page, int size, string? authorId, DateTime? createdFrom, DateTime? createdTo, string[]? sort);
    Task<Topic?> GetTopicByIdAsync(string id);
    Task<Topic> UpdateTopicAsync(string id, UpdateTopicRequest request, string requesterId);
    Task DeleteTopicAsync(string id, string requesterId);
}