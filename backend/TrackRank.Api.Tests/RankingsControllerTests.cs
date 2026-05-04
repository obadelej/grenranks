using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackRank.Api.Controllers;
using TrackRank.Api.Data;
using TrackRank.Api.Models;

namespace TrackRank.Api.Tests;

public class RankingsControllerTests
{
    [Fact]
    public async Task Get_ReturnsBadRequest_WhenEventIdIsMissing()
    {
        await using var db = CreateDbContext();
        var controller = new RankingsController(db);

        var response = await controller.Get(0, "Male", "U20", DateTime.UtcNow.Year, false);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("eventId is required.", badRequest.Value);
    }

    [Fact]
    public async Task Get_ReturnsBadRequest_WhenGenderIsMissing()
    {
        await using var db = CreateDbContext();
        var controller = new RankingsController(db);

        var response = await controller.Get(1, "", "U20", DateTime.UtcNow.Year, false);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("gender is required.", badRequest.Value);
    }

    [Fact]
    public async Task Get_ReturnsBadRequest_WhenCategoryIsMissing()
    {
        await using var db = CreateDbContext();
        var controller = new RankingsController(db);

        var response = await controller.Get(1, "Male", "", DateTime.UtcNow.Year, false);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("category is required.", badRequest.Value);
    }

    [Fact]
    public async Task Get_ReturnsBadRequest_WhenGenderIsInvalid()
    {
        await using var db = CreateDbContext();
        var eventItem = new Event { Name = "100m", EventType = "Track" };
        db.Events.Add(eventItem);
        await db.SaveChangesAsync();

        var controller = new RankingsController(db);
        var response = await controller.Get(eventItem.Id, "Unknown", "U20", DateTime.UtcNow.Year, false);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("Invalid gender. Use Male/Female or M/F.", badRequest.Value);
    }

    [Fact]
    public async Task Get_ReturnsBadRequest_WhenCategoryIsInvalid()
    {
        await using var db = CreateDbContext();
        var eventItem = new Event { Name = "100m", EventType = "Track" };
        db.Events.Add(eventItem);
        await db.SaveChangesAsync();

        var controller = new RankingsController(db);
        var response = await controller.Get(eventItem.Id, "Male", "U18", DateTime.UtcNow.Year, false);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("Invalid category. Use U7, U9, U11, U13, U15, U17, U20, or 20 Plus.", badRequest.Value);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenEventDoesNotExist()
    {
        await using var db = CreateDbContext();
        var controller = new RankingsController(db);

        var response = await controller.Get(9999, "Male", "U20", DateTime.UtcNow.Year, false);

        var notFound = Assert.IsType<NotFoundObjectResult>(response);
        Assert.Equal("Event not found.", notFound.Value);
    }

    [Fact]
    public async Task Get_TrackEvent_SortsAscending_AndAssignsTieRanks()
    {
        await using var db = CreateDbContext();
        var trackEvent = new Event { Name = "400m", EventType = "Track" };
        var meet = new Meet { Name = "Championship", Location = "Stadium", MeetDate = DateTime.UtcNow.Date };
        var athleteA = BuildAthlete("A", "Runner", "Male", yearsOldAtDec31: 18);
        var athleteB = BuildAthlete("B", "Runner", "Male", yearsOldAtDec31: 18);
        var athleteC = BuildAthlete("C", "Runner", "Male", yearsOldAtDec31: 18);
        db.AddRange(trackEvent, meet, athleteA, athleteB, athleteC);
        await db.SaveChangesAsync();

        db.Results.AddRange(
            BuildResult(athleteA, meet, trackEvent, 50.10m, DateTime.UtcNow.Date),
            BuildResult(athleteB, meet, trackEvent, 49.50m, DateTime.UtcNow.Date),
            BuildResult(athleteC, meet, trackEvent, 49.50m, DateTime.UtcNow.Date.AddDays(1)));
        await db.SaveChangesAsync();

        var controller = new RankingsController(db);
        var response = await controller.Get(trackEvent.Id, "Male", "U20", DateTime.UtcNow.Year, false);
        var json = ExtractJson(response);

        Assert.Equal("Track", json.RootElement.GetProperty("EventType").GetString());
        var rankings = json.RootElement.GetProperty("Rankings").EnumerateArray().ToList();
        Assert.Equal(3, rankings.Count);

        Assert.Equal(49.50m, rankings[0].GetProperty("Performance").GetDecimal());
        Assert.Equal(49.50m, rankings[1].GetProperty("Performance").GetDecimal());
        Assert.Equal(50.10m, rankings[2].GetProperty("Performance").GetDecimal());

        Assert.Equal(1, rankings[0].GetProperty("Rank").GetInt32());
        Assert.Equal(1, rankings[1].GetProperty("Rank").GetInt32());
        Assert.Equal(3, rankings[2].GetProperty("Rank").GetInt32());
    }

    [Fact]
    public async Task Get_FieldEvent_SortsDescending()
    {
        await using var db = CreateDbContext();
        var fieldEvent = new Event { Name = "LJ", EventType = "Field" };
        var meet = new Meet { Name = "Championship", Location = "Stadium", MeetDate = DateTime.UtcNow.Date };
        var athleteA = BuildAthlete("A", "Jumper", "Female", yearsOldAtDec31: 15);
        var athleteB = BuildAthlete("B", "Jumper", "Female", yearsOldAtDec31: 15);
        db.AddRange(fieldEvent, meet, athleteA, athleteB);
        await db.SaveChangesAsync();

        db.Results.AddRange(
            BuildResult(athleteA, meet, fieldEvent, 5.10m, DateTime.UtcNow.Date),
            BuildResult(athleteB, meet, fieldEvent, 5.45m, DateTime.UtcNow.Date));
        await db.SaveChangesAsync();

        var controller = new RankingsController(db);
        var response = await controller.Get(fieldEvent.Id, "F", "U17", DateTime.UtcNow.Year, false);
        var json = ExtractJson(response);

        Assert.True(json.RootElement.GetProperty("WindSplitRankings").GetBoolean());
        var legal = json.RootElement.GetProperty("RankingsLegalWind").EnumerateArray().ToList();
        var other = json.RootElement.GetProperty("RankingsNoWindOrIllegalWind").EnumerateArray().ToList();
        Assert.Empty(legal);
        Assert.Equal(2, other.Count);
        Assert.Equal(5.45m, other[0].GetProperty("Performance").GetDecimal());
        Assert.Equal(5.10m, other[1].GetProperty("Performance").GetDecimal());
    }

    [Fact]
    public async Task Get_BestPerAthleteOnly_KeepsOnlyBestResult()
    {
        await using var db = CreateDbContext();
        var trackEvent = new Event { Name = "800m", EventType = "Track" };
        var meet = new Meet { Name = "Championship", Location = "Stadium", MeetDate = DateTime.UtcNow.Date };
        var athleteA = BuildAthlete("A", "Middle", "Male", yearsOldAtDec31: 19);
        var athleteB = BuildAthlete("B", "Middle", "Male", yearsOldAtDec31: 19);
        db.AddRange(trackEvent, meet, athleteA, athleteB);
        await db.SaveChangesAsync();

        db.Results.AddRange(
            BuildResult(athleteA, meet, trackEvent, 121.50m, DateTime.UtcNow.Date),
            BuildResult(athleteA, meet, trackEvent, 119.90m, DateTime.UtcNow.Date.AddDays(2)),
            BuildResult(athleteB, meet, trackEvent, 122.00m, DateTime.UtcNow.Date.AddDays(1)));
        await db.SaveChangesAsync();

        var controller = new RankingsController(db);
        var response = await controller.Get(trackEvent.Id, "M", "U20", DateTime.UtcNow.Year, true);
        var json = ExtractJson(response);

        var rankings = json.RootElement.GetProperty("Rankings").EnumerateArray().ToList();
        Assert.Equal(2, rankings.Count);

        var athleteNames = rankings.Select(x => x.GetProperty("AthleteName").GetString()).ToList();
        Assert.Contains("A Middle", athleteNames);
        Assert.Contains("B Middle", athleteNames);
        Assert.Equal(119.90m, rankings.First(x => x.GetProperty("AthleteName").GetString() == "A Middle").GetProperty("Performance").GetDecimal());
    }

    [Fact]
    public async Task Get_AppliesYearAndCategoryFilters_U20And20Plus()
    {
        await using var db = CreateDbContext();
        var trackEvent = new Event { Name = "1500m", EventType = "Track" };
        var meetCurrent = new Meet { Name = "Current Meet", Location = "Stadium", MeetDate = DateTime.UtcNow.Date };
        var meetPrevious = new Meet { Name = "Previous Meet", Location = "Stadium", MeetDate = DateTime.UtcNow.Date.AddYears(-1) };

        var u20Athlete = BuildAthlete("U20", "Athlete", "Male", yearsOldAtDec31: 19);
        var seniorAthlete = BuildAthlete("Senior", "Athlete", "Male", yearsOldAtDec31: 20);
        var missingDobAthlete = new Athlete { FirstName = "Missing", LastName = "Dob", Gender = "Male", DateOfBirth = null };
        db.AddRange(trackEvent, meetCurrent, meetPrevious, u20Athlete, seniorAthlete, missingDobAthlete);
        await db.SaveChangesAsync();

        var currentYear = DateTime.UtcNow.Year;
        db.Results.AddRange(
            BuildResult(u20Athlete, meetCurrent, trackEvent, 250.00m, new DateTime(currentYear, 3, 1)),
            BuildResult(seniorAthlete, meetCurrent, trackEvent, 240.00m, new DateTime(currentYear, 3, 1)),
            BuildResult(u20Athlete, meetPrevious, trackEvent, 245.00m, new DateTime(currentYear - 1, 3, 1)),
            BuildResult(missingDobAthlete, meetCurrent, trackEvent, 255.00m, new DateTime(currentYear, 3, 1)));
        await db.SaveChangesAsync();

        var controller = new RankingsController(db);

        var u20Response = await controller.Get(trackEvent.Id, "Male", "U20", currentYear, false);
        var u20Json = ExtractJson(u20Response);
        var u20Rankings = u20Json.RootElement.GetProperty("Rankings").EnumerateArray().ToList();
        Assert.Single(u20Rankings);
        Assert.Equal("U20 Athlete", u20Rankings[0].GetProperty("AthleteName").GetString());
        Assert.Equal(1, u20Json.RootElement.GetProperty("MissingDobCount").GetInt32());

        var seniorResponse = await controller.Get(trackEvent.Id, "Male", "20 Plus", currentYear, false);
        var seniorJson = ExtractJson(seniorResponse);
        var seniorRankings = seniorJson.RootElement.GetProperty("Rankings").EnumerateArray().ToList();
        Assert.Single(seniorRankings);
        Assert.Equal("Senior Athlete", seniorRankings[0].GetProperty("AthleteName").GetString());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static Athlete BuildAthlete(string firstName, string lastName, string gender, int yearsOldAtDec31)
    {
        var currentYear = DateTime.UtcNow.Year;
        // DOB chosen so age at Dec 31 of current year is exactly yearsOldAtDec31.
        var dateOfBirth = new DateTime(currentYear - yearsOldAtDec31, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return new Athlete
        {
            FirstName = firstName,
            LastName = lastName,
            Gender = gender,
            DateOfBirth = dateOfBirth
        };
    }

    [Fact]
    public async Task Get_100m_WindSplit_SeparatesLegalAndNoWindOrIllegal()
    {
        await using var db = CreateDbContext();
        var trackEvent = new Event { Name = "100m", EventType = "Track" };
        var meet = new Meet { Name = "Championship", Location = "Stadium", MeetDate = DateTime.UtcNow.Date };
        var athleteA = BuildAthlete("A", "Sprinter", "Male", yearsOldAtDec31: 18);
        var athleteB = BuildAthlete("B", "Sprinter", "Male", yearsOldAtDec31: 18);
        var athleteC = BuildAthlete("C", "Sprinter", "Male", yearsOldAtDec31: 18);
        db.AddRange(trackEvent, meet, athleteA, athleteB, athleteC);
        await db.SaveChangesAsync();

        db.Results.AddRange(
            BuildResult(athleteA, meet, trackEvent, 10.50m, DateTime.UtcNow.Date, wind: 1.0m),
            BuildResult(athleteB, meet, trackEvent, 10.30m, DateTime.UtcNow.Date, wind: null),
            BuildResult(athleteC, meet, trackEvent, 10.20m, DateTime.UtcNow.Date, wind: 3.0m));
        await db.SaveChangesAsync();

        var controller = new RankingsController(db);
        var response = await controller.Get(trackEvent.Id, "Male", "U20", DateTime.UtcNow.Year, false);
        var json = ExtractJson(response);

        Assert.True(json.RootElement.GetProperty("WindSplitRankings").GetBoolean());
        var legal = json.RootElement.GetProperty("RankingsLegalWind").EnumerateArray().ToList();
        var other = json.RootElement.GetProperty("RankingsNoWindOrIllegalWind").EnumerateArray().ToList();
        Assert.Single(legal);
        Assert.Equal(10.50m, legal[0].GetProperty("Performance").GetDecimal());
        Assert.Equal(2, other.Count);
        Assert.Equal(10.20m, other[0].GetProperty("Performance").GetDecimal());
        Assert.Equal(10.30m, other[1].GetProperty("Performance").GetDecimal());
    }

    private static Result BuildResult(Athlete athlete, Meet meet, Event eventItem, decimal performance, DateTime resultDate, decimal? wind = null)
    {
        return new Result
        {
            Athlete = athlete,
            Meet = meet,
            Event = eventItem,
            AthleteId = athlete.Id,
            MeetId = meet.Id,
            EventId = eventItem.Id,
            Performance = performance,
            Wind = wind,
            ResultDate = DateTime.SpecifyKind(resultDate, DateTimeKind.Utc),
            SourceType = "Test",
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static JsonDocument ExtractJson(IActionResult actionResult)
    {
        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var json = JsonSerializer.Serialize(ok.Value);
        return JsonDocument.Parse(json);
    }
}
