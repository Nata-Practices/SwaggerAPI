using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApplication1;

public class Model
{
    public class Event
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } // Используем строковый Id без привязки к ObjectId
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
    }

    public class Ticket
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } // Аналогично, используем строковый Id
        public string EventId { get; set; }
        public string BuyerName { get; set; }
        public DateTime PurchaseDate { get; set; }
    }
}