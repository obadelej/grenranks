using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackRank.Api.Controllers.Dtos;
using TrackRank.Api.Data;
using TrackRank.Api.Models;

namespace TrackRank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AthletesController : ControllerBase
{
    private readonly AppDbContext _db;

    public AthletesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var athletes = await _db.Athletes
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ToListAsync();

        return Ok(athletes);
    }

    [HttpGet("missing-dob")]
    public async Task<IActionResult> GetMissingDob([FromQuery] int eventId, [FromQuery] string? gender)
    {
        if (eventId <= 0)
            return BadRequest("eventId is required.");

        var query = _db.Results
            .Where(r => r.EventId == eventId && r.Athlete.DateOfBirth == null);

        if (!string.IsNullOrWhiteSpace(gender))
        {
            var normalizedGender = NormalizeGender(gender);
            if (normalizedGender is null)
                return BadRequest("Invalid gender. Use Male/Female or M/F.");

            query = normalizedGender == "male"
                ? query.Where(r => r.Athlete.Gender == "Male" || r.Athlete.Gender == "M")
                : query.Where(r => r.Athlete.Gender == "Female" || r.Athlete.Gender == "F");
        }

        var athletes = await query
            .Select(r => new
            {
                r.Athlete.Id,
                r.Athlete.FirstName,
                r.Athlete.LastName,
                r.Athlete.Gender
            })
            .Distinct()
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ToListAsync();

        return Ok(athletes);
    }

    private static string? NormalizeGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
            return null;

        var normalized = gender.Trim().ToLowerInvariant();
        return normalized switch
        {
            "male" => "male",
            "m" => "male",
            "female" => "female",
            "f" => "female",
            _ => null
        };
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var athlete = await _db.Athletes.FindAsync(id);
        return athlete is null ? NotFound() : Ok(athlete);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAthleteRequest request)
    {
        var athlete = new Athlete
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth
        };

        _db.Athletes.Add(athlete);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = athlete.Id }, athlete);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateAthleteRequest request)
    {
        var athlete = await _db.Athletes.FindAsync(id);
        if (athlete is null)
            return NotFound();

        athlete.FirstName = request.FirstName;
        athlete.LastName = request.LastName;
        athlete.Gender = request.Gender;
        athlete.DateOfBirth = request.DateOfBirth;

        await _db.SaveChangesAsync();
        return Ok(athlete);
    }

    [HttpPut("{id:int}/date-of-birth")]
    public async Task<IActionResult> UpdateDateOfBirth(int id, [FromBody] UpdateAthleteDobRequest request)
    {
        var athlete = await _db.Athletes.FindAsync(id);
        if (athlete is null)
            return NotFound();

        athlete.DateOfBirth = request.DateOfBirth;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            athlete.Id,
            athlete.FirstName,
            athlete.LastName,
            athlete.Gender,
            athlete.DateOfBirth
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var athlete = await _db.Athletes.FindAsync(id);
        if (athlete is null)
            return NotFound();

        _db.Athletes.Remove(athlete);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}