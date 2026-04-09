using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CIS.DataAcces.Models;

public class Idea
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column("topic_id")]
    public string TopicId { get; set; } = string.Empty;

    [ForeignKey(nameof(TopicId))]
    public Topic Topic { get; set; } = null!;

    [Required]
    [Column("author_id")]
    public string AuthorId { get; set; } = string.Empty;

    [Column("vote_count")]
    public int VoteCount { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}