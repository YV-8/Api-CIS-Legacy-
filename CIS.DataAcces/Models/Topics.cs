using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CIS.DataAcces.Models;
public enum TopicStatus { draft, active, inactive, archived }
public enum TopicType { election, market_research, survey, other }

public class Topic
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString(); //UUID()
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    public TopicType Type { get; set; }
    
    public string VoteType { get; set; } = "single";
    
    public bool AllowComments { get; set; } = true;
    
    public bool AnonymousVote { get; set; } = false;
    
    [Required]
    public TopicStatus Status { get; set; } = TopicStatus.draft;
    
    [Required]
    [Column("user_id")]
    public string AuthorId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; } // Para el Soft Delete
}