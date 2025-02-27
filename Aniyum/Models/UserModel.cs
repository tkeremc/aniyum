using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aniyum.Models;

public class UserModel : BaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string? FullName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? HashedPassword { get; set; }
    public bool IsActive { get; set; }

    public List<string>? Roles { get; set; }
}