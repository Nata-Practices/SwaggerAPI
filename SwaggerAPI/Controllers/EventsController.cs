using Microsoft.AspNetCore.Mvc;
using SwaggerAPI.Services;

namespace SwaggerAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllEvents() => Ok(await _eventService.GetAllEventsAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEventById(string id)
    {
        var evnt = await _eventService.GetEventByIdAsync(id);
        return evnt is not null ? Ok(evnt) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> AddEvent([FromBody] EventModel newEvent)
    {
        await _eventService.AddEventAsync(newEvent);
        return CreatedAtAction(nameof(GetEventById), new { id = newEvent.Id }, newEvent);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(string id, [FromBody] EventModel updatedEvent)
    {
        var updated = await _eventService.UpdateEventAsync(id, updatedEvent);
        return updated != null ? Ok(updated) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(string id)
    {
        var eventToRemove = await _eventService.DeleteEventAsync(id);
        return eventToRemove != null ? Ok(eventToRemove) : NotFound();
    }
    
    [HttpDelete("all")]
    public async Task<IActionResult> DeleteAllEvents()
    {
        var events = await _eventService.GetAllEventsAsync();
        foreach (var evnt in events)
        {
            await _eventService.DeleteEventAsync(evnt.Id);
        }
        return NoContent();
    }
}