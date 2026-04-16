namespace CIS.BusinessLogic.Domain;

public sealed class CommentDetails
{
    public string Id { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string IdeaId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
