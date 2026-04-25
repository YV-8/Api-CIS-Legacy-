using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CIS.DataAcces.MongoDB.Documents;

public class VoteDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? MongoId { get; set; }

    public Guid Id { get; set; }
    public Guid IdeaId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
} 