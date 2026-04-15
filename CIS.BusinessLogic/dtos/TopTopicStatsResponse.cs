namespace CIS.BusinessLogic.dtos;

public record TopTopicStatsResponse
{
    public string TopicId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int IdeaCount { get; init; }
    public int TotalVotes { get; init; }
}
