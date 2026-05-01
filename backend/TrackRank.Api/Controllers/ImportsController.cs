using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using TrackRank.Api.Controllers.Dtos;
using TrackRank.Api.Data;
using TrackRank.Api.Models;
using TrackRank.Api.Services.Import;

namespace TrackRank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportsController : ControllerBase
{
    private const string AdminHeaderName = "X-Admin-Key";
    private readonly IHytekCsvParser _hytekCsvParser;
    private readonly AppDbContext _db;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _configuration;

    public ImportsController(IHytekCsvParser hytekCsvParser, AppDbContext db, IHostEnvironment env, IConfiguration configuration)
    {
        _hytekCsvParser = hytekCsvParser;
        _db = db;
        _env = env;
        _configuration = configuration;
    }

    [HttpPost("hytek")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> ImportHytek([FromForm] ImportHytekFileRequest request, CancellationToken cancellationToken)
    {
        if (!IsAuthorizedAdminRequest())
            return Unauthorized(new { message = $"Hy-Tek import requires a valid {AdminHeaderName} header outside Development/Testing." });

        IFormFile? file = request.File;

        // Fallback for Swagger/form-data binding quirks.
        if ((file is null || file.Length == 0) &&
            Request.HasFormContentType &&
            Request.Form.Files.Count > 0)
        {
            file = Request.Form.Files[0];
        }

        if (file is null || file.Length == 0)
            return BadRequest("CSV file is required.");

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .csv files are supported.");

        await using var stream = file.OpenReadStream();
        var parseResult = await _hytekCsvParser.ParseAsync(stream, cancellationToken);

        var importedCount = 0;
        var skippedCount = 0;
        var importErrors = new List<string>(parseResult.Errors);

        Meet? meet = null;
        if (!string.IsNullOrWhiteSpace(parseResult.ParsedMeetName))
        {
            var meetDate = parseResult.ParsedMeetDate ?? DateTime.UtcNow.Date;
            meet = await _db.Meets.FirstOrDefaultAsync(m =>
                m.Name == parseResult.ParsedMeetName && m.MeetDate.Date == meetDate.Date, cancellationToken);

            if (meet is null)
            {
                meet = new Meet
                {
                    Name = parseResult.ParsedMeetName,
                    MeetDate = meetDate,
                    Location = parseResult.ParsedComment ?? string.Empty
                };
                _db.Meets.Add(meet);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        foreach (var row in parseResult.Rows)
        {
            try
            {
                var athlete = await _db.Athletes.FirstOrDefaultAsync(a =>
                    a.FirstName == row.AthleteFirstName &&
                    a.LastName == row.AthleteLastName &&
                    a.Gender == row.Gender, cancellationToken);

                if (athlete is null)
                {
                    athlete = new Athlete
                    {
                        FirstName = row.AthleteFirstName,
                        LastName = row.AthleteLastName,
                        Gender = row.Gender,
                        DateOfBirth = row.DateOfBirth
                    };
                    _db.Athletes.Add(athlete);
                    await _db.SaveChangesAsync(cancellationToken);
                }
                else if (athlete.DateOfBirth is null && row.DateOfBirth is not null)
                {
                    athlete.DateOfBirth = row.DateOfBirth;
                    await _db.SaveChangesAsync(cancellationToken);
                }

                var eventItem = await _db.Events.FirstOrDefaultAsync(e =>
                    e.Name == row.EventName && e.EventType == row.EventType, cancellationToken);

                if (eventItem is null)
                {
                    eventItem = new Event
                    {
                        Name = row.EventName,
                        EventType = row.EventType
                    };
                    _db.Events.Add(eventItem);
                    await _db.SaveChangesAsync(cancellationToken);
                }

                var effectiveMeet = meet;
                if (effectiveMeet is null)
                {
                    var meetName = string.IsNullOrWhiteSpace(row.MeetName) ? "Imported Meet" : row.MeetName;
                    var meetDate = row.MeetDate ?? row.ResultDate.Date;
                    effectiveMeet = await _db.Meets.FirstOrDefaultAsync(m =>
                        m.Name == meetName && m.MeetDate.Date == meetDate.Date, cancellationToken);
                    if (effectiveMeet is null)
                    {
                        effectiveMeet = new Meet
                        {
                            Name = meetName,
                            MeetDate = meetDate,
                            Location = row.MeetLocation ?? string.Empty
                        };
                        _db.Meets.Add(effectiveMeet);
                        await _db.SaveChangesAsync(cancellationToken);
                    }
                }

                var duplicateExists = await _db.Results.AnyAsync(r =>
                    r.AthleteId == athlete.Id &&
                    r.EventId == eventItem.Id &&
                    r.MeetId == effectiveMeet.Id &&
                    r.ResultDate == row.ResultDate &&
                    r.Performance == row.Performance, cancellationToken);

                if (duplicateExists)
                {
                    skippedCount++;
                    continue;
                }

                _db.Results.Add(new Result
                {
                    AthleteId = athlete.Id,
                    EventId = eventItem.Id,
                    MeetId = effectiveMeet.Id,
                    Performance = row.Performance,
                    Wind = row.Wind,
                    ResultDate = row.ResultDate,
                    SourceType = "HytekImport",
                    CreatedAtUtc = DateTime.UtcNow
                });
                importedCount++;
            }
            catch (Exception ex)
            {
                skippedCount++;
                importErrors.Add($"Failed to import {row.AthleteFirstName} {row.AthleteLastName} / {row.EventName}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var trackPreview = parseResult.Rows
            .Where(r => string.Equals(r.EventType, "Track", StringComparison.OrdinalIgnoreCase))
            .Take(10);

        var fieldPreview = parseResult.Rows
            .Where(r => string.Equals(r.EventType, "Field", StringComparison.OrdinalIgnoreCase))
            .Take(10);

        var previewRows = trackPreview
            .Concat(fieldPreview)
            .ToList();

        var trackParsedCount = parseResult.Rows.Count(r =>
            string.Equals(r.EventType, "Track", StringComparison.OrdinalIgnoreCase));
        var fieldParseCount = parseResult.Rows.Count(r =>
            string.Equals(r.EventType, "Field", StringComparison.OrdinalIgnoreCase));

        // Fallback for files that only contain one event type.
        if (previewRows.Count == 0)
        {
            previewRows = parseResult.Rows.Take(20).ToList();
        }

        var importHistory = new ImportHistory
        {
            FileName = file.FileName,
            TotalRows = parseResult.TotalRows,
            ParsedRows = parseResult.ParsedRows,
            ImportedCount = importedCount,
            SkippedCount = skippedCount,
            ErrorCount = importErrors.Count,
            TrackParsedCount = trackParsedCount,
            FieldParseCount = fieldParseCount,
            ImportedAtUtc = DateTime.UtcNow
        };
        _db.ImportHistories.Add(importHistory);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            FileName = file.FileName,
            parseResult.TotalRows,
            parseResult.ParsedRows,
            ImportedCount = importedCount,
            SkippedCount = skippedCount,
            ErrorCount = importErrors.Count,
            TrackParsedCount = trackParsedCount,
            FieldParseCount = fieldParseCount,
            parseResult.MeetHeaderRaw,
            parseResult.ParsedMeetName,
            parseResult.ParsedMeetDate,
            parseResult.ParsedMeetEndDate,
            parseResult.ParsedFileCreatedBy,
            parseResult.ParsedDateCreated,
            parseResult.ParsedComment,
            Errors = importErrors,
            PreviewRows = previewRows
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int take = 25, CancellationToken cancellationToken = default)
    {
        if (take <= 0 || take > 200)
            return BadRequest("take must be between 1 and 200.");

        var history = await _db.ImportHistories
            .OrderByDescending(x => x.ImportedAtUtc)
            .Take(take)
            .Select(x => new
            {
                x.Id,
                x.FileName,
                x.TotalRows,
                x.ParsedRows,
                x.ImportedCount,
                x.SkippedCount,
                x.ErrorCount,
                x.TrackParsedCount,
                x.FieldParseCount,
                x.ImportedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(history);
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
