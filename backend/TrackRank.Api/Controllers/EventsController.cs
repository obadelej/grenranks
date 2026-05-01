using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackRank.Api.Controllers.Dtos;
using TrackRank.Api.Data;
using TrackRank.Api.Models;

namespace TrackRank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var events = await _db.Events
            .OrderBy(e => e.Name)
            .ToListAsync();

        return Ok(events);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var eventItem = await _db.Events.FindAsync(id);
        return eventItem is null ? NotFound() : Ok(eventItem);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var eventItem = new Event
        {
            Name = request.Name,
            EventType = request.EventType
        };

        _db.Events.Add(eventItem);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = eventItem.Id }, eventItem);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateEventRequest request)
    {
        var eventItem = await _db.Events.FindAsync(id);
        if (eventItem is null)
            return NotFound();

        eventItem.Name = request.Name;
        eventItem.EventType = request.EventType;

        await _db.SaveChangesAsync();
        return Ok(eventItem);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var eventItem = await _db.Events.FindAsync(id);
        if (eventItem is null)
            return NotFound();

        _db.Events.Remove(eventItem);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}