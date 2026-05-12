using System.ComponentModel.DataAnnotations;

namespace CIS.BusinessLogic.dtos;

public record CreateCommentRequest
{
    [Required(ErrorMessage = "Content is required")]
    [StringLength(1000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;
}
