namespace CIS.BusinessLogic.dtos;

public record IdeaResponse
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string AuthorId { get; init; } = string.Empty;
    public string TopicId { get; init; } = string.Empty;
    public int VoteCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public object[] Links { get; init; } = Array.Empty<object>();
}