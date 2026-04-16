using System.ComponentModel.DataAnnotations;

namespace CIS.BusinessLogic.dtos;

public record UpdateTopicRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, MinimumLength = 5)]
    public string Title { get; init; } = string.Empty;

    [StringLength(1000, MinimumLength = 10)]
    public string? Description { get; init; }

    public bool? AllowComments { get; init; }

    public bool? AnonymousVote { get; init; }
}
