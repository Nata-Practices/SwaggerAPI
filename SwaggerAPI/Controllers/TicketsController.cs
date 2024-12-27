using Microsoft.AspNetCore.Mvc;
using SwaggerAPI.Services;

namespace SwaggerAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllTickets() => Ok(await _ticketService.GetAllTicketsAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicketById(string id)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);
        return ticket is not null ? Ok(ticket) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> BuyTicket([FromBody] TicketModel newTicket)
    {
        newTicket.PurchaseDate = DateTime.UtcNow;
        await _ticketService.BuyTicketAsync(newTicket);
        return CreatedAtAction(nameof(GetTicketById), new { id = newTicket.Id }, newTicket);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTicket(string id, [FromBody] string buyerName)
    {
        var updatedTicket = await _ticketService.UpdateTicketAsync(id, buyerName);
        return updatedTicket is not null ? Ok(updatedTicket) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTicket(string id)
    {
        var ticket = await _ticketService.ReturnTicketAsync(id);
        return ticket is not null ? Ok(ticket) : NotFound();
    }
    
    [HttpDelete("all")]
    public async Task<IActionResult> DeleteAllTickets()
    {
        var events = await _ticketService.GetAllTicketsAsync();
        foreach (var evnt in events)
        {
            await _ticketService.ReturnTicketAsync(evnt.Id);
        }
        return NoContent();
    }
}