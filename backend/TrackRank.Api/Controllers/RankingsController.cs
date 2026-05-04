using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackRank.Api.Data;
using TrackRank.Api.Services;

namespace TrackRank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RankingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RankingsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int eventId,
        [FromQuery] string gender,
        [FromQuery] string category,
        [FromQuery] int? year,
        [FromQuery] bool bestPerAthleteOnly = false)
    {
        if (eventId <= 0)
            return BadRequest("eventId is required.");
        if (string.IsNullOrWhiteSpace(gender))
            return BadRequest("gender is required.");
        if (string.IsNullOrWhiteSpace(category))
            return BadRequest("category is required.");

        var eventInfo = await _db.Events
            .Where(e => e.Id == eventId)
            .Select(e => new { e.Id, e.Name, e.EventType })
            .FirstOrDefaultAsync();

        if (eventInfo is null)
            return NotFound("Event not found.");

        var baseResults = await _db.Results
            .Where(r => r.EventId == eventId)
            .Select(r => new
            {
                r.Id,
                r.AthleteId,
                AthleteName = r.Athlete.FirstName + " " + r.Athlete.LastName,
                AthleteGender = r.Athlete.Gender,
                DateOfBirth = r.Athlete.DateOfBirth,
                r.MeetId,
                MeetName = r.Meet.Name,
                r.Performance,
                r.Wind,
                r.ResultDate
            })
            .ToListAsync();

        var rows = baseResults
            .Select(r => new WindRankingRow(
                r.Id,
                r.AthleteId,
                r.AthleteName,
                r.AthleteGender,
                r.DateOfBirth,
                r.MeetId,
                r.MeetName,
                r.Performance,
                r.Wind,
                r.ResultDate))
            .ToList();

        var normalizedGender = NormalizeGender(gender);
        if (normalizedGender is null)
            return BadRequest("Invalid gender. Use Male/Female or M/F.");
        var normalizedCategory = NormalizeCategory(category);
        if (normalizedCategory is null)
            return BadRequest("Invalid category. Use U7, U9, U11, U13, U15, U17, U20, or 20 Plus.");

        var genderFiltered = rows
            .Where(r => NormalizeGender(r.AthleteGender) == normalizedGender)
            .ToList();

        var missingDobCount = genderFiltered.Count(r => r.DateOfBirth is null);

        var currentYear = DateTime.UtcNow.Year;
        var referenceDate = new DateTime(currentYear, 12, 31);
        var filtered = genderFiltered
            .Where(r => IsInCategory(r.DateOfBirth, referenceDate, normalizedCategory))
            .ToList();

        if (year.HasValue)
        {
            filtered = filtered
                .Where(r => r.ResultDate.Year == year.Value)
                .ToList();
        }

        var normalizedType = eventInfo.EventType.ToLowerInvariant();
        var isTrack = normalizedType == "track";
        var windSplit = UsesWindSplitRankings(eventInfo.Name, eventInfo.EventType);

        if (windSplit)
        {
            var legalWind = filtered.Where(r => r.Wind.HasValue && r.Wind.Value <= 2.0m).ToList();
            var noWindOrIllegal = filtered.Where(r => !r.Wind.HasValue || r.Wind.Value > 2.0m).ToList();

            if (bestPerAthleteOnly)
            {
                legalWind = ApplyBestPerAthlete(legalWind, isTrack);
                noWindOrIllegal = ApplyBestPerAthlete(noWindOrIllegal, isTrack);
            }

            var rankingsLegalWind = BuildRankings(legalWind, isTrack);
            var rankingsNoWindOrIllegalWind = BuildRankings(noWindOrIllegal, isTrack);

            return Ok(new
            {
                EventId = eventInfo.Id,
                EventName = eventInfo.Name,
                EventDisplayName = EventNameFormatter.ToDisplayName(eventInfo.Name),
                EventType = eventInfo.EventType,
                Gender = gender,
                Category = normalizedCategory,
                Year = year,
                BestPerAthleteOnly = bestPerAthleteOnly,
                ReferenceDate = referenceDate,
                MissingDobCount = missingDobCount,
                WindSplitRankings = true,
                WindSplitNote =
                    "Legal wind: wind reading present and ≤ +2.0 m/s. Other list: no wind reading or wind > +2.0 m/s.",
                Warning = missingDobCount > 0
                    ? $"{missingDobCount} athlete result(s) excluded because date of birth is missing."
                    : null,
                Rankings = Array.Empty<object>(),
                RankingsLegalWind = rankingsLegalWind,
                RankingsNoWindOrIllegalWind = rankingsNoWindOrIllegalWind
            });
        }

        if (bestPerAthleteOnly)
            filtered = ApplyBestPerAthlete(filtered, isTrack);

        var rankings = BuildRankings(filtered, isTrack);

        return Ok(new
        {
            EventId = eventInfo.Id,
            EventName = eventInfo.Name,
            EventDisplayName = EventNameFormatter.ToDisplayName(eventInfo.Name),
            EventType = eventInfo.EventType,
            Gender = gender,
            Category = normalizedCategory,
            Year = year,
            BestPerAthleteOnly = bestPerAthleteOnly,
            ReferenceDate = referenceDate,
            MissingDobCount = missingDobCount,
            WindSplitRankings = false,
            Warning = missingDobCount > 0
                ? $"{missingDobCount} athlete result(s) excluded because date of birth is missing."
                : null,
            Rankings = rankings
        });
    }

    private static List<WindRankingRow> ApplyBestPerAthlete(
        List<WindRankingRow> rows,
        bool isTrack)
    {
        return rows
            .GroupBy(r => new { r.AthleteId, r.AthleteName, r.AthleteGender, r.DateOfBirth })
            .Select(g => isTrack
                ? g.OrderBy(x => x.Performance).ThenBy(x => x.ResultDate).First()
                : g.OrderByDescending(x => x.Performance).ThenBy(x => x.ResultDate).First())
            .ToList();
    }

    private static List<object> BuildRankings(List<WindRankingRow> rows, bool isTrack)
    {
        var ordered = isTrack
            ? rows.OrderBy(r => r.Performance).ThenBy(r => r.ResultDate).ToList()
            : rows.OrderByDescending(r => r.Performance).ThenBy(r => r.ResultDate).ToList();

        var rankings = new List<object>();
        decimal? previousPerformance = null;
        var currentRank = 0;

        for (var i = 0; i < ordered.Count; i++)
        {
            var row = ordered[i];
            if (previousPerformance is null || row.Performance != previousPerformance.Value)
            {
                currentRank = i + 1;
                previousPerformance = row.Performance;
            }

            rankings.Add(new
            {
                Rank = currentRank,
                row.Id,
                row.AthleteId,
                row.AthleteName,
                row.MeetId,
                row.MeetName,
                row.Performance,
                row.Wind,
                row.ResultDate
            });
        }

        return rankings;
    }

    private static bool UsesWindSplitRankings(string eventName, string eventType)
    {
        var display = EventNameFormatter.ToDisplayName(eventName).Trim();

        if (string.Equals(display, "Long Jump", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(display, "Triple Jump", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(display, "100m Hurdles", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(display, "110m Hurdles", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.Equals(eventType, "Track", StringComparison.OrdinalIgnoreCase))
            return false;

        var raw = eventName.Trim();
        if (IsRelayEventName(raw) || IsRelayEventName(display))
            return false;

        var meters = TryParseSprintDistanceMetersUpTo200(raw) ?? TryParseSprintDistanceMetersUpTo200(display);
        return meters is > 0 and <= 200;
    }

    private static bool IsRelayEventName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        if (name.Contains("relay", StringComparison.OrdinalIgnoreCase))
            return true;
        return Regex.IsMatch(name, @"\d+\s*x\s*\d+", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Finds a sprint distance (1–200 m) from the event name, e.g. "100m", "Girls 100m", "100 M".
    /// Does not treat longer distances like 1500m as sprints (value greater than 200).
    /// </summary>
    private static int? TryParseSprintDistanceMetersUpTo200(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        foreach (Match m in Regex.Matches(name, @"\b(\d+)\s*m\b", RegexOptions.IgnoreCase))
        {
            if (!int.TryParse(m.Groups[1].Value, out var v) || v <= 0)
                continue;
            if (v <= 200)
                return v;
        }

        return null;
    }

    private sealed record WindRankingRow(
        int Id,
        int AthleteId,
        string AthleteName,
        string AthleteGender,
        DateTime? DateOfBirth,
        int MeetId,
        string MeetName,
        decimal Performance,
        decimal? Wind,
        DateTime ResultDate);

    private static string? NormalizeCategory(string category)
    {
        var normalized = category.Trim().ToUpperInvariant();
        return normalized switch
        {
            "U7" => "U7",
            "U9" => "U9",
            "U11" => "U11",
            "U13" => "U13",
            "U15" => "U15",
            "U17" => "U17",
            "U20" => "U20",
            "20 PLUS" => "20 Plus",
            "20PLUS" => "20 Plus",
            _ => null
        };
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

    private static bool IsInCategory(DateTime? dateOfBirth, DateTime referenceDate, string category)
    {
        if (dateOfBirth is null)
            return false;

        var age = CalculateAge(dateOfBirth.Value, referenceDate);
        return category switch
        {
            "U7" => age >= 0 && age <= 6,
            "U9" => age is 7 or 8,
            "U11" => age is 9 or 10,
            "U13" => age is 11 or 12,
            "U15" => age is 13 or 14,
            "U17" => age is 15 or 16,
            "U20" => age >= 17 && age <= 19,
            "20 Plus" => age >= 20,
            _ => false
        };
    }

    private static int CalculateAge(DateTime dateOfBirth, DateTime onDate)
    {
        var age = onDate.Year - dateOfBirth.Year;
        if (onDate.Date < dateOfBirth.Date.AddYears(age))
            age--;
        return age;
    }
}
