using CIS.DataAcces.Models;

namespace CIS.BusinessLogic.dtos;

public record TopicResponse
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string AuthorId { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public TopicStatus Status { get; init; }
    public object[] Links { get; init; } = Array.Empty<object>();
}