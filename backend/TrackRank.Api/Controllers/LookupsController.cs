using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackRank.Api.Data;

namespace TrackRank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LookupsController : ControllerBase
{
    private readonly AppDbContext _db;

    public LookupsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("athletes")]
    public async Task<IActionResult> GetAthletes()
    {
        var athletes = await _db.Athletes
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .Select(a => new
            {
                a.Id,
                Name = a.FirstName + " " + a.LastName
            })
            .ToListAsync();

        return Ok(athletes);
    }

    [HttpGet("meets")]
    public async Task<IActionResult> GetMeets()
    {
        var meets = await _db.Meets
            .OrderByDescending(m => m.MeetDate)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.MeetDate
            })
            .ToListAsync();

        return Ok(meets);
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _db.Events
            .OrderBy(e => e.Name)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.EventType
            })
            .ToListAsync();

        return Ok(events);
    }
}
