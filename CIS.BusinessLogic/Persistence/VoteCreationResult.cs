namespace CIS.BusinessLogic.Persistence;

public sealed class VoteCreationResult
{
    public string VoteId { get; init; } = string.Empty;
    public string IdeaId { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public string TopicId { get; init; } = string.Empty;
}
