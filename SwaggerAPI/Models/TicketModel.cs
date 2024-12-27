using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SwaggerAPI.Models;

/// <summary>
/// Модель билета.
/// </summary>
public class TicketModel
{
    /// <summary>
    /// Уникальный идентификатор события, используется как ключ в базе данных.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; }

    /// <summary>
    /// Идентификатор события, для которого был приобретен билет.
    /// </summary>
    public string EventId { get; set; }

    /// <summary>
    /// Имя покупателя, который приобрел билет.
    /// </summary>
    public string BuyerName { get; set; }

    /// <summary>
    /// Дата и время покупки билета.
    /// </summary>
    public DateTime PurchaseDate { get; set; }
}