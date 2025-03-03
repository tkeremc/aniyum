using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aniyum.Models;

public class RefreshTokenModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string? UserId { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime Expiration { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsUsed { get; set; }
    public string? Ip { get; set; }
    public string? DeviceId { get; set; }
}