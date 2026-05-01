using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using TrackRank.Api.Data;
using TrackRank.Api.Models;

namespace TrackRank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHostEnvironment _env;

    public SeedController(AppDbContext db, IHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> Seed()
    {
        if (!IsDevelopmentOrTestingEnvironment())
            return StatusCode(403, new { message = "Seed is only available in Development (or Testing for automated tests)." });

        if (!await _db.Athletes.AnyAsync())
        {
            _db.Athletes.Add(new Athlete { FirstName = "John", LastName = "Doe", Gender = "Male" });
            _db.Meets.Add(new Meet { Name = "Spring Invitational", Location = "Boston", MeetDate = DateTime.UtcNow.Date });
            _db.Events.Add(new Event { Name = "100m", EventType = "Track" });
            await _db.SaveChangesAsync();
        }

        return Ok("Seed complete");
    }

    private bool IsDevelopmentOrTestingEnvironment() =>
        _env.IsDevelopment() ||
        string.Equals(_env.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);
}