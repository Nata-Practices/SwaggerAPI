using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
// Для атрибутов валидации

namespace SwaggerAPI.Models;

/// <summary>
/// Модель события.
/// </summary>
public class EventModel
{
    /// <summary>
    /// Уникальный идентификатор события, используется как ключ в базе данных.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    [Required(ErrorMessage = "Поле 'Id' обязательно для заполнения.")]
    public string Id { get; set; }

    /// <summary>
    /// Название события.
    /// </summary>
    [Required(ErrorMessage = "Поле 'Name' обязательно для заполнения.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Название должно быть от 3 до 100 символов.")]
    public string Name { get; set; }

    /// <summary>
    /// Описание события.
    /// </summary>
    [Required(ErrorMessage = "Поле 'Description' обязательно для заполнения.")]
    public string Description { get; set; }

    /// <summary>
    /// Дата и время события.
    /// </summary>
    [Required(ErrorMessage = "Поле 'Date' обязательно для заполнения.")]
    [DataType(DataType.DateTime, ErrorMessage = "Неверный формат даты.")]
    public DateTime Date { get; set; }

    /// <summary>
    /// Цена участия или билета на событие.
    /// </summary>
    [Required(ErrorMessage = "Поле 'Price' обязательно для заполнения.")]
    [Range(0, double.MaxValue, ErrorMessage = "Цена должна быть больше или равна 0.")]
    public decimal Price { get; set; }
}