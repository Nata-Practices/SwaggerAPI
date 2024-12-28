using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace SwaggerAPI.Models;

/// <summary>
/// Модель данных для представления объекта.
/// </summary>
public class ObjectModel
{
    /// <summary>
    /// Уникальный идентификатор объекта, используется как ключ в базе данных.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    [Required(ErrorMessage = "Поле 'Id' обязательно для заполнения.")]
    public string Id { get; set; }

    /// <summary>
    /// Имя объекта.
    /// </summary>
    [Required(ErrorMessage = "Поле 'Name' обязательно для заполнения.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Имя объекта должно быть от 3 до 100 символов.")]
    public string Name { get; set; }

    /// <summary>
    /// Идентификатор пользователя, который подтвердил объект.
    /// </summary>
    [Required(ErrorMessage = "Поле 'UserId' обязательно для заполнения.")]
    public string UserId { get; set; }

    /// <summary>
    /// Временная метка подтверждения объекта.
    /// </summary>
    [Required(ErrorMessage = "Поле 'ConfirmationTimestamp' обязательно для заполнения.")]
    [DefaultValue("0001-01-01T00:00:00Z")]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public DateTime ConfirmationTimestamp { get; set; } = DateTime.MinValue;
}