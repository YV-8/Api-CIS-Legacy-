using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CIS.DataAcces.Models;

public class IdeaVote
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("idea_id")]
    public string IdeaId { get; set; } = string.Empty;

    [ForeignKey(nameof(IdeaId))]
    public Idea Idea { get; set; } = null!;

    [Required]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}