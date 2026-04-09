namespace CIS.BusinessLogic.dtos;

public record PaginatedResponse<T>
{
    public IEnumerable<T> Content { get; init; } = new List<T>();
    public int Page { get; init; }
    public int Size { get; init; }
    public long TotalElements { get; init; }
    public int TotalPages { get; init; }
}