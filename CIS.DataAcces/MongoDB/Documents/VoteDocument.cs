using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CIS.DataAcces.MongoDB.Documents;

public class VoteDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? MongoId { get; set; }

    public string Id { get; set; }= Guid.NewGuid().ToString();
    public string IdeaId { get; set; }= string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
} 