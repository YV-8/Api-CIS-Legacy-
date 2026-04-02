namespace CIS.DataAcces.Models;

public enum TopicStatus { Draft, Active, Inactive, Archive }

public class Topic
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty; // JWT
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public TopicStatus Status { get; set; } = TopicStatus.Draft;
    public bool IsDeleted { get; set; } = false; // Soft Delete
}