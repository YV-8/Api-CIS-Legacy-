using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
public class IdeaDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? MongoId { get; set; }

    public string Id { get; set; }= Guid.NewGuid().ToString();
    public string TopicId { get; set; }= string.Empty;
    
    public string Title { get; set; }= string.Empty;
    public string Description { get; set; }= string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public int VoteCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}