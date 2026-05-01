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
    private const string AdminHeaderName = "X-Admin-Key";
    private readonly AppDbContext _db;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _configuration;

    public SeedController(AppDbContext db, IHostEnvironment env, IConfiguration configuration)
    {
        _db = db;
        _env = env;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> Seed()
    {
        if (!IsAuthorizedAdminRequest())
            return Unauthorized(new { message = $"Seed requires a valid {AdminHeaderName} header outside Development/Testing." });

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

    private bool IsAuthorizedAdminRequest()
    {
        if (IsDevelopmentOrTestingEnvironment())
            return true;

        var configuredKey = _configuration["Security:AdminApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
            return false;

        if (!Request.Headers.TryGetValue(AdminHeaderName, out var provided))
            return false;

        return string.Equals(provided.ToString(), configuredKey, StringComparison.Ordinal);
    }
}