namespace SwaggerAPI.Models;

/// <summary>
/// Стандартный ответ API.
/// </summary>
/// <typeparam name="T">Тип данных, возвращаемых API.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Флаг успешности выполнения запроса.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Данные, возвращаемые API.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Сообщение, описывающее результат выполнения.
    /// </summary>
    public string? Message { get; set; }
}