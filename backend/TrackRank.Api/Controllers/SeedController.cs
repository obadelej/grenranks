using Microsoft.AspNetCore.Mvc;
using TrackRank.Api.Data;
using TrackRank.Api.Models;

namespace TrackRank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly AppDbContext _db;

    public SeedController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Seed()
    {
        if (!_db.Athletes.Any())
        {
            _db.Athletes.Add(new Athlete { FirstName = "John", LastName = "Doe", Gender = "Male" });
            _db.Meets.Add(new Meet { Name = "Spring Invitational", Location = "Boston", MeetDate = DateTime.UtcNow.Date });
            _db.Events.Add(new Event { Name = "100m", EventType = "Track" });
            await _db.SaveChangesAsync();
        }

        return Ok("Seed complete");
    }
}