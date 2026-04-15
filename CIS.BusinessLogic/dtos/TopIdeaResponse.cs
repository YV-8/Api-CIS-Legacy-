namespace CIS.BusinessLogic.dtos;

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