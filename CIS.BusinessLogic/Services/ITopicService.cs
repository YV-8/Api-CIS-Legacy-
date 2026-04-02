using CIS.BusinessLogic.dtos;
using CIS.DataAcces.Models;

namespace CIS.BusinessLogic.Services;

public interface ITopicService
{
    Task<Topic> CreateTopicAsync(CreateTopicRequest request, string authorId);
}