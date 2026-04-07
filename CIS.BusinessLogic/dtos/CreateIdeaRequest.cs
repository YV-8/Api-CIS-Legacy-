using System.ComponentModel.DataAnnotations;

namespace CIS.BusinessLogic.dtos;

public record CreateIdeaRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000, MinimumLength = 5)]
    public string Description { get; init; } = string.Empty;
}