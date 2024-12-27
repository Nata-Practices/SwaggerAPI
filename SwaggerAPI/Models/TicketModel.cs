using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SwaggerAPI;

public class TicketModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } // Аналогично, используем строковый Id
    public string EventId { get; set; }
    public string BuyerName { get; set; }
    public DateTime PurchaseDate { get; set; }
}