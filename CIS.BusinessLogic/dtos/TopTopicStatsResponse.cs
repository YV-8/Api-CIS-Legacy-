namespace CIS.BusinessLogic.dtos;

public record TopTopicStatsResponse
{
    public string TopicId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int IdeaCount { get; init; }
    public int TotalVotes { get; init; }
}

public record TopIdeaResponse(
    string IdeaId, 
    string Title, 
    string TopicId, 
    string AuthorId, 
    int VoteCount
);

public record TopUserResponse(
    string UserId, 
    int ActivityCount
);
