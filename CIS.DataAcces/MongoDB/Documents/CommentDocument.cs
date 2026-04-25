using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
public class CommentDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? MongoId { get; set; }
    public Guid Id { get; set; }
    public Guid IdeaId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty; 
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}