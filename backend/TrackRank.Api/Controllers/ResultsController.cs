using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackRank.Api.Controllers.Dtos;
using TrackRank.Api.Data;
using TrackRank.Api.Models;

namespace TrackRank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var results = await _db.Results
            .Select(r => new
            {
                r.Id,
                r.AthleteId,
                AthleteName = r.Athlete.FirstName + " " + r.Athlete.LastName,
                r.MeetId,
                MeetName = r.Meet.Name,
                r.EventId,
                EventName = r.Event.Name,
                r.Performance,
                r.Wind,
                r.ResultDate,
                r.SourceType,
                r.CreatedAtUtc
            })
            .OrderByDescending(r => r.ResultDate)
            .ToListAsync();

        return Ok(results);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _db.Results
            .Where(r => r.Id == id)
            .Select(r => new
            {
                r.Id,
                r.AthleteId,
                AthleteName = r.Athlete.FirstName + " " + r.Athlete.LastName,
                r.MeetId,
                MeetName = r.Meet.Name,
                r.EventId,
                EventName = r.Event.Name,
                r.Performance,
                r.Wind,
                r.ResultDate,
                r.SourceType,
                r.CreatedAtUtc
            })
            .FirstOrDefaultAsync();

        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateResultRequest request)
    {
        var athleteExists = await _db.Athletes.AnyAsync(a => a.Id == request.AthleteId);
        var meetExists = await _db.Meets.AnyAsync(m => m.Id == request.MeetId);
        var eventExists = await _db.Events.AnyAsync(e => e.Id == request.EventId);

        if (!athleteExists || !meetExists || !eventExists)
            return BadRequest("AthleteId, MeetId, or EventId is invalid.");

        var duplicateExists = await _db.Results.AnyAsync(r =>
            r.AthleteId == request.AthleteId &&
            r.MeetId == request.MeetId &&
            r.EventId == request.EventId);

        if (duplicateExists)
            return BadRequest("A result already exists for this athlete/meet/event.");

        var result = new Result
        {
            AthleteId = request.AthleteId,
            MeetId = request.MeetId,
            EventId = request.EventId,
            Performance = request.Performance,
            Wind = request.Wind,
            ResultDate = request.ResultDate,
            SourceType = "Manual",
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var created = await _db.Results
            .Where(r => r.Id == result.Id)
            .Select(r => new
            {
                r.Id,
                r.AthleteId,
                AthleteName = r.Athlete.FirstName + " " + r.Athlete.LastName,
                r.MeetId,
                MeetName = r.Meet.Name,
                r.EventId,
                EventName = r.Event.Name,
                r.Performance,
                r.Wind,
                r.ResultDate,
                r.SourceType,
                r.CreatedAtUtc
            })
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateResultRequest request)
    {
        var result = await _db.Results.FindAsync(id);
        if (result is null)
            return NotFound();

        var athleteExists = await _db.Athletes.AnyAsync(a => a.Id == request.AthleteId);
        var meetExists = await _db.Meets.AnyAsync(m => m.Id == request.MeetId);
        var eventExists = await _db.Events.AnyAsync(e => e.Id == request.EventId);

        if (!athleteExists || !meetExists || !eventExists)
            return BadRequest("AthleteId, MeetId, or EventId is invalid.");

        var duplicateExists = await _db.Results.AnyAsync(r =>
            r.Id != id &&
            r.AthleteId == request.AthleteId &&
            r.MeetId == request.MeetId &&
            r.EventId == request.EventId);

        if (duplicateExists)
            return BadRequest("Another result already exists for this athlete/meet/event.");

        result.AthleteId = request.AthleteId;
        result.MeetId = request.MeetId;
        result.EventId = request.EventId;
        result.Performance = request.Performance;
        result.Wind = request.Wind;
        result.ResultDate = request.ResultDate;

        await _db.SaveChangesAsync();

        var updated = await _db.Results
            .Where(r => r.Id == id)
            .Select(r => new
            {
                r.Id,
                r.AthleteId,
                AthleteName = r.Athlete.FirstName + " " + r.Athlete.LastName,
                r.MeetId,
                MeetName = r.Meet.Name,
                r.EventId,
                EventName = r.Event.Name,
                r.Performance,
                r.Wind,
                r.ResultDate,
                r.SourceType,
                r.CreatedAtUtc
            })
            .FirstAsync();

        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _db.Results.FindAsync(id);
        if (result is null)
            return NotFound();

        _db.Results.Remove(result);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}