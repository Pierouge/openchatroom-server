using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("_id")]
    public string id { get; set; } = string.Empty;
    [BsonElement("userName")]
    public string userName { get; set; } = string.Empty;
    [BsonElement("visibleName")]
    public string visibleName { get; set; } = string.Empty;
    [BsonElement("password")]
    public string password { get; set; } = string.Empty;
    [BsonElement("salt")]
    public string salt { get; set; } = string.Empty;
    [BsonElement("friends")]
    public string[] friends { get; set; } = Array.Empty<string>();
}