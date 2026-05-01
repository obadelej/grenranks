using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackRank.Api.Controllers.Dtos;
using TrackRank.Api.Data;
using TrackRank.Api.Models;

namespace TrackRank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeetsController : ControllerBase
{
    private readonly AppDbContext _db;

    public MeetsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var meets = await _db.Meets
            .OrderByDescending(m => m.MeetDate)
            .ToListAsync();

        return Ok(meets);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var meet = await _db.Meets.FindAsync(id);
        return meet is null ? NotFound() : Ok(meet);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMeetRequest request)
    {
        var meet = new Meet
        {
            Name = request.Name,
            Location = request.Location,
            MeetDate = request.MeetDate
        };

        _db.Meets.Add(meet);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = meet.Id }, meet);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateMeetRequest request)
    {
        var meet = await _db.Meets.FindAsync(id);
        if (meet is null)
            return NotFound();

        meet.Name = request.Name;
        meet.Location = request.Location;
        meet.MeetDate = request.MeetDate;

        await _db.SaveChangesAsync();
        return Ok(meet);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var meet = await _db.Meets.FindAsync(id);
        if (meet is null)
            return NotFound();

        _db.Meets.Remove(meet);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}