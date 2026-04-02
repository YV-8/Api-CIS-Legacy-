using System.ComponentModel.DataAnnotations;

namespace CIS.BusinessLogic.dtos;

public record CreateTopicRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, MinimumLength = 5)]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(1000, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;
}