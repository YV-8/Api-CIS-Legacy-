namespace CIS.BusinessLogic.dtos;

public record TopicFilterRequest
{
    public int Page { get; init; } = 0;
    public int Size { get; init; } = 10;
    public string? AuthorId { get; init; }
    public string? CreatedFrom { get; init; }
    public string? CreatedTo { get; init; }
    public string[]? Sort { get; set; }
}