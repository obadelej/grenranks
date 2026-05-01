using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackRank.Api.Controllers.Dtos;
using TrackRank.Api.Data;
using TrackRank.Api.Models;
using TrackRank.Api.Services;

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
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] int? athleteId = null,
        [FromQuery] int? eventId = null,
        [FromQuery] int? year = null,
        [FromQuery] string? sourceType = null,
        [FromQuery] string sortBy = "resultDate",
        [FromQuery] string sortDir = "desc")
    {
        if (page <= 0)
            return BadRequest("page must be greater than 0.");
        if (pageSize <= 0 || pageSize > 200)
            return BadRequest("pageSize must be between 1 and 200.");
        if (year.HasValue && (year.Value < 1900 || year.Value > 3000))
            return BadRequest("year must be between 1900 and 3000.");

        var query = _db.Results
            .Select(r => new
            {
                r.Id,
                r.AthleteId,
                AthleteName = r.Athlete.FirstName + " " + r.Athlete.LastName,
                r.MeetId,
                MeetName = r.Meet.Name,
                r.EventId,
                EventName = r.Event.Name,
                EventDisplayName = EventNameFormatter.ToDisplayName(r.Event.Name),
                r.Performance,
                r.Wind,
                r.ResultDate,
                r.SourceType,
                r.CreatedAtUtc
            });

        if (athleteId.HasValue)
            query = query.Where(r => r.AthleteId == athleteId.Value);
        if (eventId.HasValue)
            query = query.Where(r => r.EventId == eventId.Value);
        if (year.HasValue)
            query = query.Where(r => r.ResultDate.Year == year.Value);
        if (!string.IsNullOrWhiteSpace(sourceType))
            query = query.Where(r => r.SourceType.ToLower() == sourceType.Trim().ToLower());

        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
        var normalizedSortDir = sortDir.Trim().ToLowerInvariant();
        var isDesc = normalizedSortDir != "asc";
        if (normalizedSortDir is not ("asc" or "desc"))
            return BadRequest("sortDir must be 'asc' or 'desc'.");
        if (normalizedSortBy is not ("resultdate" or "performance" or "createdatutc" or "athletename" or "eventname"))
            return BadRequest("sortBy must be one of: resultDate, performance, createdAtUtc, athleteName, eventName.");

        query = normalizedSortBy switch
        {
            "resultdate" => isDesc
                ? query.OrderByDescending(r => r.ResultDate).ThenBy(r => r.Id)
                : query.OrderBy(r => r.ResultDate).ThenBy(r => r.Id),
            "performance" => isDesc
                ? query.OrderByDescending(r => r.Performance).ThenBy(r => r.Id)
                : query.OrderBy(r => r.Performance).ThenBy(r => r.Id),
            "createdatutc" => isDesc
                ? query.OrderByDescending(r => r.CreatedAtUtc).ThenBy(r => r.Id)
                : query.OrderBy(r => r.CreatedAtUtc).ThenBy(r => r.Id),
            "athletename" => isDesc
                ? query.OrderByDescending(r => r.AthleteName).ThenBy(r => r.Id)
                : query.OrderBy(r => r.AthleteName).ThenBy(r => r.Id),
            "eventname" => isDesc
                ? query.OrderByDescending(r => r.EventName).ThenBy(r => r.Id)
                : query.OrderBy(r => r.EventName).ThenBy(r => r.Id),
            _ => query
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
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
                EventDisplayName = EventNameFormatter.ToDisplayName(r.Event.Name),
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
                EventDisplayName = EventNameFormatter.ToDisplayName(r.Event.Name),
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
                EventDisplayName = EventNameFormatter.ToDisplayName(r.Event.Name),
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