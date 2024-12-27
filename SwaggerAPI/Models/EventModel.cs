using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SwaggerAPI;

public class EventModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } // Используем строковый Id без привязки к ObjectId
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
}