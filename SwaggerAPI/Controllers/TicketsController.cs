using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SwaggerAPI.Models;
using SwaggerAPI.Services;

namespace SwaggerAPI.Controllers;

/// <summary>
/// Контроллер для управления билетами.
/// </summary>
[ApiController]
[Route("api/tickets")]
[Produces("application/json")]
[Tags("Управление билетами")]
public class TicketsController(ITicketService ticketService) : ControllerBase
{
    /// <summary>
    /// Получить список всех билетов.
    /// </summary>
    /// <returns>Список билетов.</returns>
    /// <response code="200">Успешный ответ с данными билетов.</response>
    /// <response code="401">Вы не авторизованы</response>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllTickets()
    {
        var tickets = await ticketService.GetAllTicketsAsync();
        return Ok(new ApiResponse<IEnumerable<TicketModel>>
        {
            Success = true,
            Data = tickets,
            Message = "Билеты успешно получены."
        });
    }

    /// <summary>
    /// Получить билет по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор билета.</param>
    /// <returns>Информация о билете.</returns>
    /// <response code="200">Успешный ответ с данными билета.</response>
    /// <response code="401">Вы не авторизованы</response>
    /// <response code="404">Билет с указанным идентификатором не найден.</response>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetTicketById(string id)
    {
        var ticket = await ticketService.GetTicketByIdAsync(id);
        if (ticket != null)
        {
            return Ok(new ApiResponse<TicketModel>
            {
                Success = true,
                Data = ticket,
                Message = "Билет успешно получен."
            });
        }

        return NotFound(new ApiResponse<string>
        {
            Success = false,
            Message = $"Билет №{id} не найден!"
        });
    }

    /// <summary>
    /// Добавить билет.
    /// </summary>
    /// <param name="newTicket">Модель нового билета.</param>
    /// <returns>Созданный билет.</returns>
    /// <response code="201">Билет успешно создан.</response>
    /// <response code="400">Неверный формат данных.</response>
    /// <response code="401">Вы не авторизованы</response>
    /// <response code="409">Билет c таким id уже существует.</response>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> BuyTicket([FromBody] TicketModel newTicket)
    {
        try
        {
            newTicket.PurchaseDate = DateTime.UtcNow;
            await ticketService.BuyTicketAsync(newTicket);
            return CreatedAtAction(nameof(GetTicketById), new { id = newTicket.Id }, new ApiResponse<TicketModel>
            {
                Success = true,
                Data = newTicket,
                Message = "Билет успешно создан."
            });
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return Conflict(new ApiResponse<string>
            {
                Success = false,
                Message = $"Билет с id: {newTicket.Id} уже существует!"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Произошла ошибка при создании билета. Пожалуйста, попробуйте снова позже."
            });
        }
    }

    /// <summary>
    /// Обновить информацию о билете.
    /// </summary>
    /// <param name="id">Идентификатор билета.</param>
    /// <param name="buyerName">Новое имя покупателя.</param>
    /// <returns>Обновленный билет.</returns>
    /// <response code="200">Билет успешно обновлен.</response>
    /// <response code="401">Вы не авторизованы</response>
    /// <response code="404">Билет с указанным идентификатором не найден.</response>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateTicket(string id, [FromBody] string buyerName)
    {
        var updatedTicket = await ticketService.UpdateTicketAsync(id, buyerName);
        if (updatedTicket != null)
        {
            return Ok(new ApiResponse<TicketModel>
            {
                Success = true,
                Data = updatedTicket,
                Message = "Билет успешно обновлен."
            });
        }

        return NotFound(new ApiResponse<string>
        {
            Success = false,
            Message = $"Билет №{id} не найден!"
        });
    }

    /// <summary>
    /// Удалить билет.
    /// </summary>
    /// <param name="id">Идентификатор билета.</param>
    /// <returns>Информация о возвращенном билете.</returns>
    /// <response code="200">Билет успешно удален.</response>
    /// <response code="401">Вы не авторизованы</response>
    /// <response code="404">Билет с указанным идентификатором не найден.</response>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteTicket(string id)
    {
        var ticket = await ticketService.ReturnTicketAsync(id);
        if (ticket != null)
        {
            return Ok(new ApiResponse<TicketModel>
            {
                Success = true,
                Data = ticket,
                Message = "Билет успешно возвращен."
            });
        }

        return NotFound(new ApiResponse<string>
        {
            Success = false,
            Message = $"Билет №{id} не найден!"
        });
    }
    
    /// <summary>
    /// Удалить все билеты.
    /// </summary>
    /// <returns>Нет содержимого.</returns>
    /// <response code="204">Все билеты успешно удалены.</response>
    /// <response code="401">Вы не авторизованы</response>
    [HttpDelete("all")]
    [Authorize]
    public async Task<IActionResult> DeleteAllTickets()
    {
        var tickets = await ticketService.GetAllTicketsAsync();
        foreach (var ticket in tickets)
        {
            await ticketService.ReturnTicketAsync(ticket.Id);
        }

        return Ok(new ApiResponse<string>
        {
            Success = true,
            Data = null,
            Message = "Все билеты успешно удалены."
        });
    }
}