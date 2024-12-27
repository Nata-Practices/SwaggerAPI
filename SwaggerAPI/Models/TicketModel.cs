using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SwaggerAPI.Models;

/// <summary>
/// Модель билета.
/// </summary>
public class TicketModel
{
    /// <summary>
    /// Уникальный идентификатор билета, используется как ключ в базе данных.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    [Required(ErrorMessage = "Поле 'Id' обязательно для заполнения.")]
    public string Id { get; set; }

    /// <summary>
    /// Идентификатор события, для которого был приобретен билет.
    /// </summary>
    [Required(ErrorMessage = "Поле 'EventId' обязательно для заполнения.")]
    public string EventId { get; set; }

    /// <summary>
    /// Имя покупателя, который приобрел билет.
    /// </summary>
    [Required(ErrorMessage = "Поле 'BuyerName' обязательно для заполнения.")]
    [StringLength(100, ErrorMessage = "Имя покупателя не должно превышать 100 символов.")]
    public string BuyerName { get; set; }

    /// <summary>
    /// Дата и время покупки билета.
    /// </summary>
    [Required(ErrorMessage = "Поле 'PurchaseDate' обязательно для заполнения.")]
    [DataType(DataType.DateTime, ErrorMessage = "Неверный формат даты.")]
    public DateTime PurchaseDate { get; set; }
}