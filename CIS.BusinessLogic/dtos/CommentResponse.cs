namespace CIS.BusinessLogic.dtos;

public record CommentResponse
{
    public string Id { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string IdeaId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public object[] Links { get; init; } = Array.Empty<object>();
}
