namespace CIS.BusinessLogic.Domain;

public sealed class TopicDetails
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string AuthorId { get; init; } = string.Empty;
    public TopicType Type { get; init; }
    public TopicStatus Status { get; init; }
    public string VoteType { get; init; } = "single";
    public bool AllowComments { get; init; }
    public bool AnonymousVote { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}
