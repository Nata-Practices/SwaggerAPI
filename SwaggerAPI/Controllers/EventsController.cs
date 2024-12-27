using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SwaggerAPI.Models;
using SwaggerAPI.Services;

namespace SwaggerAPI.Controllers;

/// <summary>
/// Контроллер для управления событиями.
/// </summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
[Tags("Управление событиями")]
public class EventsController(IEventService eventService) : ControllerBase
{
    /// <summary>
    /// Получить список всех событий.
    /// </summary>
    /// <returns>Список событий</returns>
    /// <response code="200">Успешный ответ с данными событий</response>
    [HttpGet]
    public async Task<IActionResult> GetAllEvents()
    {
        var events = await eventService.GetAllEventsAsync();
        return Ok(new ApiResponse<IEnumerable<EventModel>>
        {
            Success = true,
            Data = events ?? new List<EventModel>(),
            Message = "События успешно получены."
        });
    }
    
    /// <summary>
    /// Получить событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    /// <returns>Информация о событии</returns>
    /// <response code="200">Успешный ответ с данными события</response>
    /// <response code="404">Событие с таким идентификатором не найдено</response>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEventById(string id)
    {
        var _event = await eventService.GetEventByIdAsync(id);
        if (_event != null)
        {
            return Ok(new ApiResponse<EventModel>
            {
                Success = true,
                Data = _event,
                Message = "Событие успешно получено."
            });
        }

        return NotFound(new ApiResponse<string>
        {
            Success = false,
            Message = $"Ивент №{id} не найден!"
        });
    }
    
    /// <summary>
    /// Добавить новое событие.
    /// </summary>
    /// <param name="newEvent">Модель нового события</param>
    /// <returns>Созданное событие</returns>
    /// <response code="201">Событие успешно создано</response>
    /// <response code="400">Неверный формат данных</response>
    /// <response code="409">Событие c таким id уже существует</response>
    [HttpPost]
    public async Task<IActionResult> AddEvent([FromBody] EventModel newEvent)
    {
        try
        {
            await eventService.AddEventAsync(newEvent);
            return CreatedAtAction(nameof(GetEventById), new { id = newEvent.Id }, new ApiResponse<EventModel>
            {
                Success = true,
                Data = newEvent,
                Message = "Событие успешно создано."
            });
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return Conflict(new ApiResponse<string>
            {
                Success = false,
                Message = $"Объект с id: {newEvent.Id} уже существует!"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Произошла ошибка: " + ex.Message
            });
        }
    }
    
    /// <summary>
    /// Обновить событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    /// <param name="updatedEvent">Модель обновленного события</param>
    /// <returns>Обновленное событие</returns>
    /// <response code="200">Событие успешно обновлено</response>
    /// <response code="404">Событие с таким идентификатором не найдено</response>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(string id, [FromBody] EventModel updatedEvent)
    {
        var updated = await eventService.UpdateEventAsync(id, updatedEvent);
        if (updated != null)
        {
            return Ok(new ApiResponse<EventModel>
            {
                Success = true,
                Data = updated,
                Message = "Событие успешно обновлено."
            });
        }

        return NotFound(new ApiResponse<string>
        {
            Success = false,
            Message = $"Ивент №{id} не найден!"
        });
    }
    
    /// <summary>
    /// Удалить событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    /// <returns>Удаленное событие</returns>
    /// <response code="200">Событие успешно удалено</response>
    /// <response code="404">Событие с таким идентификатором не найдено</response>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(string id)
    {
        var eventToRemove = await eventService.DeleteEventAsync(id);
        if (eventToRemove != null)
        {
            return Ok(new ApiResponse<EventModel>
            {
                Success = true,
                Data = null,
                Message = "Событие успешно удалено."
            });
        }

        return NotFound(new ApiResponse<string>
        {
            Success = false,
            Message = $"Ивент №{id} не найден!"
        });
    }
    
    /// <summary>
    /// Удалить все события.
    /// </summary>
    /// <returns>Нет содержимого</returns>
    /// <response code="204">Все события успешно удалены</response>
    [HttpDelete("all")]
    public async Task<IActionResult> DeleteAllEvents()
    {
        var events = await eventService.GetAllEventsAsync();
        foreach (var evnt in events)
        {
            await eventService.DeleteEventAsync(evnt.Id);
        }

        return Ok(new ApiResponse<string>
        {
            Success = true,
            Data = null,
            Message = "Все события успешно удалены."
        });
    }
}