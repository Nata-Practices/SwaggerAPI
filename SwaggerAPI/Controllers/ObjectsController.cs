using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SwaggerAPI.Models;
using SwaggerAPI.Services;

namespace SwaggerAPI.Controllers;

/// <summary>
/// Контроллер для управления объектами.
/// </summary>
[ApiController]
[Route("api/objects")]
[Produces("application/json")]
[Tags("Управление объектами")]
public class ObjectsController(IObjectService objectService) : ControllerBase
{
    
    /// <summary>
    /// Получить список всех объектов.
    /// </summary>
    /// <returns>Список объектов.</returns>
    /// <response code="200">Успешный ответ с данными объектов.</response>
    /// <response code="401">Вы не авторизованы</response>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetObjects()
    {
        var objects = await objectService.GetObjectsAsync();
        return Ok(new ApiResponse<IEnumerable<ObjectModel>>
        {
            Success = true,
            Data = objects,
            Message = "Объекты успешно получены."
        });
    }
    
    /// <summary>
    /// Создать новый объект.
    /// </summary>
    /// <param name="objectModel">Модель нового объекта.</param>
    /// <returns>Созданный объект.</returns>
    /// <response code="201">Объект успешно создан.</response>
    /// <response code="400">Некорректные данные для создания объекта.</response>
    /// <response code="401">Вы не авторизованы</response>
    /// <response code="409">Объект с таким ID уже существует.</response>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateObject([FromBody] ObjectModel objectModel)
    {
        try
        {
            await objectService.CreateObjectAsync(objectModel);
            return CreatedAtAction(nameof(GetObjectById), new { id = objectModel.Id }, new ApiResponse<ObjectModel>
            {
                Success = true,
                Data = objectModel,
                Message = "Объект успешно создан."
            });
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return Conflict(new ApiResponse<string>
            {
                Success = false,
                Message = $"Объект с ID {objectModel.Id} уже существует!"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Произошла ошибка при создании объекта: " + ex.Message
            });
        }
    }
    
    /// <summary>
    /// Получить объект по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор объекта.</param>
    /// <returns>Информация об объекте.</returns>
    /// <response code="200">Успешный ответ с данными объекта.</response>
    /// <response code="401">Вы не авторизованы</response>
    /// <response code="404">Объект с указанным идентификатором не найден.</response>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetObjectById(string id)
    {
        var obj = await objectService.GetObjectByIdAsync(id);
        if (obj != null)
        {
            return Ok(new ApiResponse<ObjectModel>
            {
                Success = true,
                Data = obj,
                Message = "Объект успешно получен."
            });
        }

        return NotFound(new ApiResponse<string>
        {
            Success = false,
            Message = $"Объект с ID {id} не найден!"
        });
    }

    /// <summary>
    /// Удалить объект.
    /// </summary>
    /// <param name="id">Идентификатор объекта.</param>
    /// <returns>Результат операции.</returns>
    /// <response code="200">Объект успешно удален.</response>
    /// <response code="401">Вы не авторизованы</response>
    /// <response code="404">Объект с указанным идентификатором не найден.</response>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteObject(string id)
    {
        var obj = await objectService.DeleteObjectAsync(id);
        if (obj != null)
        {
            return Ok(new ApiResponse<ObjectModel>
            {
                Success = true,
                Data = obj,
                Message = "Объект успешно удален."
            });
        }

        return NotFound(new ApiResponse<string>
        {
            Success = false,
            Message = $"Объект с ID {id} не найден!"
        });
    }
    
    /// <summary>
    /// Удалить все объекты.
    /// </summary>
    /// <returns>Нет содержимого.</returns>
    /// <response code="204">Все объекты успешно удалены.</response>
    /// <response code="401">Вы не авторизованы</response>
    [HttpDelete("all")]
    [Authorize]
    public async Task<IActionResult> DeleteAllTickets()
    {
        var objects = await objectService.GetObjectsAsync();
        foreach (var _object in objects)
        {
            await objectService.DeleteObjectAsync(_object.Id);
        }

        return Ok(new ApiResponse<string>
        {
            Success = true,
            Data = null,
            Message = "Все объекты успешно удалены."
        });
    }
}