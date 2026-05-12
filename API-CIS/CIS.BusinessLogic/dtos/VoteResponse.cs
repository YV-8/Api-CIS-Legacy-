namespace CIS.BusinessLogic.dtos;

public record VoteResponse
{
    public string Id { get; init; } = string.Empty;
    public string IdeaId { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public object[] Links { get; init; } = Array.Empty<object>();
}
