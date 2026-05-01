using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TrackRank.Api.Services.Import;

public interface IHytekCsvParser
{
    Task<HytekImportParseResult> ParseAsync(Stream csvStream, CancellationToken cancellationToken = default);
}

public class HytekCsvParser : IHytekCsvParser
{
    private static readonly HashSet<string> IgnoredMarks = new(StringComparer.OrdinalIgnoreCase)
    {
        "DNS", "DNF", "FS", "NT", "DQ", "ND", "NH", "FOUL", "SCR"
    };

    private static readonly string[] RequiredHeaders =
    {
        "AthleteFirstName",
        "AthleteLastName",
        "Gender",
        "EventName",
        "EventType",
        "Performance",
        "ResultDate"
    };

    public async Task<HytekImportParseResult> ParseAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(csvStream);
        var content = await reader.ReadToEndAsync();
        var delimiter = DetectDelimiter(content);
        var nonEmptyLines = content
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        // Hy-Tek export: H header line + E record lines.
        if (ContainsHytekERecord(nonEmptyLines, delimiter))
        {
            return ParseHytekERecordContent(content, delimiter, cancellationToken);
        }

        return await ParseHeaderBasedContent(content, delimiter, cancellationToken);
    }

    private static async Task<HytekImportParseResult> ParseHeaderBasedContent(
        string content,
        string delimiter,
        CancellationToken cancellationToken)
    {
        var rows = new List<HytekImportRow>();
        var errors = new List<string>();

        using var contentReader = new StringReader(content);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            Delimiter = delimiter
        };

        using var csv = new CsvReader(contentReader, config);
        await csv.ReadAsync();
        csv.ReadHeader();

        var headers = csv.HeaderRecord ?? Array.Empty<string>();
        var missingHeaders = RequiredHeaders.Where(h => !headers.Contains(h, StringComparer.OrdinalIgnoreCase)).ToList();
        if (missingHeaders.Count > 0)
        {
            return new HytekImportParseResult
            {
                TotalRows = 0,
                ParsedRows = 0,
                Errors = new List<string>
                {
                    $"Missing required header(s): {string.Join(", ", missingHeaders)}"
                },
                Rows = rows
            };
        }

        var rowNumber = 1;
        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;

            try
            {
                var row = new HytekImportRow
                {
                    AthleteFirstName = Get(csv, "AthleteFirstName"),
                    AthleteLastName = Get(csv, "AthleteLastName"),
                    Gender = Get(csv, "Gender"),
                    DateOfBirth = ParseNullableDate(Get(csv, "DateOfBirth")),
                    EventName = Get(csv, "EventName"),
                    EventType = Get(csv, "EventType"),
                    Performance = ParseDecimal(Get(csv, "Performance"), rowNumber, "Performance"),
                    Wind = ParseNullableDecimal(Get(csv, "Wind"), rowNumber, "Wind"),
                    ResultDate = ParseDate(Get(csv, "ResultDate"), rowNumber, "ResultDate"),
                    MeetName = Get(csv, "MeetName"),
                    MeetLocation = Get(csv, "MeetLocation"),
                    MeetDate = ParseNullableDate(Get(csv, "MeetDate"))
                };

                ValidateRow(row, rowNumber);
                rows.Add(row);
            }
            catch (Exception ex)
            {
                errors.Add($"Row {rowNumber}: {ex.Message}");
            }
        }

        return new HytekImportParseResult
        {
            TotalRows = rows.Count + errors.Count,
            ParsedRows = rows.Count,
            Errors = errors,
            Rows = rows
        };
    }

    private static HytekImportParseResult ParseHytekERecordContent(
        string content,
        string delimiter,
        CancellationToken cancellationToken)
    {
        var rows = new List<HytekImportRow>();
        var errors = new List<string>();

        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var firstNonEmptyLine = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim() ?? string.Empty;
        var headerFields = firstNonEmptyLine.Split(delimiter);
        var header = ParseHeaderRecord(headerFields);
        var lineNumber = 0;

        foreach (var rawLine in lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lineNumber++;

            var line = rawLine?.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = line.Split(delimiter);
            if (fields.Length < 33)
            {
                // Ignore intro lines like "Results Export Record - Individual Event"
                continue;
            }

            if (string.Equals(fields[0].Trim(), "H", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.Equals(fields[0].Trim(), "E", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                // Field positions from your specification are 1-based.
                var eventTypeCode = GetByIndex(fields, 2);
                if (!string.Equals(eventTypeCode, "T", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(eventTypeCode, "F", StringComparison.OrdinalIgnoreCase))
                {
                    // Per requirement, only Track/Field individual results are imported.
                    continue;
                }

                var eventCode = GetByIndex(fields, 5);
                var eventGender = GetByIndex(fields, 6);
                var resultMark = GetByIndex(fields, 11);
                if (ShouldIgnoreMark(resultMark))
                {
                    continue;
                }
                var wind = GetByIndex(fields, 13);
                var lastName = GetByIndex(fields, 23);
                var firstName = GetByIndex(fields, 24);
                var athleteGender = GetByIndex(fields, 26);
                var birthDate = GetByIndex(fields, 27);
                var teamName = GetByIndex(fields, 29);

                var parsedResultDate = header.MeetStartDate ?? DateTime.UtcNow.Date;
                var parsedPerformance = ParseResultMark(resultMark, eventTypeCode);

                var row = new HytekImportRow
                {
                    AthleteFirstName = firstName,
                    AthleteLastName = lastName,
                    Gender = NormalizeGender(athleteGender, eventGender),
                    DateOfBirth = ParseNullableDateFlexible(birthDate),
                    EventName = eventCode,
                    EventType = MapEventType(eventTypeCode),
                    Performance = parsedPerformance,
                    Wind = ParseNullableDecimalSafe(wind),
                    ResultDate = parsedResultDate,
                    MeetName = string.IsNullOrWhiteSpace(teamName) ? header.MeetName : teamName,
                    MeetLocation = string.Empty,
                    MeetDate = header.MeetStartDate
                };

                ValidateRow(row, lineNumber);
                rows.Add(row);
            }
            catch (Exception ex)
            {
                errors.Add($"Line {lineNumber}: {ex.Message}");
            }
        }

        return new HytekImportParseResult
        {
            TotalRows = rows.Count + errors.Count,
            ParsedRows = rows.Count,
            Errors = errors,
            Rows = rows,
            MeetHeaderRaw = firstNonEmptyLine,
            ParsedMeetName = header.MeetName,
            ParsedMeetDate = header.MeetStartDate,
            ParsedMeetEndDate = header.MeetEndDate,
            ParsedFileCreatedBy = header.FileCreatedBy,
            ParsedDateCreated = header.DateCreated,
            ParsedComment = header.Comment
        };
    }

    private static string Get(CsvReader csv, string headerName) =>
        csv.GetField(headerName)?.Trim() ?? string.Empty;

    private static DateTime ParseDate(string value, int row, string field)
    {
        if (!DateTime.TryParse(value, out var parsed))
            throw new InvalidOperationException($"{field} is not a valid date.");

        return EnsureUtc(parsed);
    }

    private static DateTime? ParseNullableDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return DateTime.TryParse(value, out var parsed) ? EnsureUtc(parsed) : null;
    }

    private static DateTime? ParseNullableDateFlexible(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var formats = new[] { "MM/dd/yyyy", "M/d/yyyy", "yyyy-MM-dd" };
        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return EnsureUtc(exact);

        return DateTime.TryParse(value, out var parsed) ? EnsureUtc(parsed) : null;
    }

    private static decimal ParseDecimal(string value, int row, string field)
    {
        if (!decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            throw new InvalidOperationException($"{field} is not a valid decimal.");

        return parsed;
    }

    private static decimal? ParseNullableDecimal(string value, int row, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            throw new InvalidOperationException($"{field} is not a valid decimal.");

        return parsed;
    }

    private static decimal? ParseNullableDecimalSafe(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static void ValidateRow(HytekImportRow row, int rowNumber)
    {
        if (string.IsNullOrWhiteSpace(row.AthleteFirstName))
            throw new InvalidOperationException("AthleteFirstName is required.");
        if (string.IsNullOrWhiteSpace(row.AthleteLastName))
            throw new InvalidOperationException("AthleteLastName is required.");
        if (string.IsNullOrWhiteSpace(row.Gender))
            throw new InvalidOperationException("Gender is required.");
        if (string.IsNullOrWhiteSpace(row.EventName))
            throw new InvalidOperationException("EventName is required.");
        if (string.IsNullOrWhiteSpace(row.EventType))
            throw new InvalidOperationException("EventType is required.");
    }

    private static string DetectDelimiter(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return ",";

        var firstLine = content
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .FirstOrDefault() ?? string.Empty;

        var semicolonCount = firstLine.Count(c => c == ';');
        var commaCount = firstLine.Count(c => c == ',');

        return semicolonCount > commaCount ? ";" : ",";
    }

    private static bool ContainsHytekERecord(List<string> nonEmptyLines, string delimiter)
    {
        if (nonEmptyLines.Count == 0)
            return false;

        foreach (var line in nonEmptyLines.Take(25))
        {
            var tokens = line.Split(delimiter);
            if (tokens.Length >= 33 && string.Equals(tokens[0].Trim(), "E", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string GetByIndex(string[] fields, int oneBasedIndex)
    {
        var idx = oneBasedIndex - 1;
        return idx >= 0 && idx < fields.Length ? fields[idx].Trim() : string.Empty;
    }

    private static string MapEventType(string eventTypeCode)
    {
        var code = eventTypeCode.Trim().ToUpperInvariant();
        return code switch
        {
            "T" or "TM" => "Track",
            "F" or "FM" => "Field",
            "M" => "Combined",
            _ => "Track"
        };
    }

    private static string NormalizeGender(string athleteGender, string eventGender)
    {
        var g = (string.IsNullOrWhiteSpace(athleteGender) ? eventGender : athleteGender).Trim().ToUpperInvariant();
        return g switch
        {
            "M" => "Male",
            "F" => "Female",
            "X" => "Mixed",
            _ => g
        };
    }

    private static decimal ParseResultMark(string resultMark, string eventTypeCode)
    {
        var value = resultMark?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Result Mark is required.");

        var normalizedEventType = eventTypeCode.Trim().ToUpperInvariant();

        if (normalizedEventType == "F")
        {
            // Field marks may include metric suffix, e.g. 6.23m
            value = Regex.Replace(value, "[mM]$", "");
        }

        // Remove trailing "h" used for hand-timed marks, keep numeric part.
        value = Regex.Replace(value, "[hH]$", "");

        // Track times: hh:mm:ss.tt / mm:ss.tt / ss.tt
        // Per Hy-Tek format, field #2 drives this: T = Track, F = Field.
        if (normalizedEventType == "T" && value.Contains(':'))
        {
            // Hy-Tek commonly uses m:ss.xx or mm:ss.xx
            var matchMinSec = Regex.Match(value, @"^(?<m>\d+):(?<s>\d{1,2})\.(?<f>\d{1,3})$");
            if (matchMinSec.Success)
            {
                var minutes = int.Parse(matchMinSec.Groups["m"].Value, CultureInfo.InvariantCulture);
                var seconds = int.Parse(matchMinSec.Groups["s"].Value, CultureInfo.InvariantCulture);
                var fractionRaw = matchMinSec.Groups["f"].Value;
                var fraction = decimal.Parse("0." + fractionRaw, CultureInfo.InvariantCulture);
                return (minutes * 60m) + seconds + fraction;
            }

            // Fallback for hour-based formats if present.
            if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var ts))
                return (decimal)ts.TotalSeconds;
        }

        // Field marks can be feet/inches format like 12'10.25; for now extract numeric fallback.
        if (normalizedEventType == "F" && value.Contains('\''))
        {
            var feetInches = value.Split('\'', StringSplitOptions.RemoveEmptyEntries);
            if (feetInches.Length >= 1)
            {
                var feet = decimal.TryParse(feetInches[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : 0m;
                var inches = feetInches.Length > 1 && decimal.TryParse(feetInches[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var i) ? i : 0m;
                return feet + (inches / 12m);
            }
        }

        if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        throw new InvalidOperationException($"Result Mark '{resultMark}' could not be parsed.");
    }

    private static bool ShouldIgnoreMark(string resultMark)
    {
        if (string.IsNullOrWhiteSpace(resultMark))
            return false;

        var normalized = resultMark.Trim().ToUpperInvariant();
        return IgnoredMarks.Contains(normalized);
    }

    private static HytekHeaderInfo ParseHeaderRecord(string[] fields)
    {
        var isHeader = string.Equals(GetByIndex(fields, 1), "H", StringComparison.OrdinalIgnoreCase);
        if (!isHeader)
        {
            return new HytekHeaderInfo();
        }

        return new HytekHeaderInfo
        {
            MeetName = GetByIndex(fields, 2),
            MeetStartDate = ParseNullableDateFlexible(GetByIndex(fields, 3)),
            MeetEndDate = ParseNullableDateFlexible(GetByIndex(fields, 4)),
            FileCreatedBy = GetByIndex(fields, 5),
            DateCreated = ParseNullableDateFlexible(GetByIndex(fields, 6)),
            Comment = fields.Length > 0 ? fields[^1].Trim() : string.Empty
        };
    }

    private static DateTime EnsureUtc(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };
    }
}

public class HytekImportRow
{
    public string AthleteFirstName { get; set; } = string.Empty;
    public string AthleteLastName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public decimal Performance { get; set; }
    public decimal? Wind { get; set; }
    public DateTime ResultDate { get; set; }
    public string MeetName { get; set; } = string.Empty;
    public string MeetLocation { get; set; } = string.Empty;
    public DateTime? MeetDate { get; set; }
}

public class HytekImportParseResult
{
    public int TotalRows { get; set; }
    public int ParsedRows { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<HytekImportRow> Rows { get; set; } = new();
    public string MeetHeaderRaw { get; set; } = string.Empty;
    public string ParsedMeetName { get; set; } = string.Empty;
    public DateTime? ParsedMeetDate { get; set; }
    public DateTime? ParsedMeetEndDate { get; set; }
    public string ParsedFileCreatedBy { get; set; } = string.Empty;
    public DateTime? ParsedDateCreated { get; set; }
    public string ParsedComment { get; set; } = string.Empty;
}

public class HytekHeaderInfo
{
    public string MeetName { get; set; } = string.Empty;
    public DateTime? MeetStartDate { get; set; }
    public DateTime? MeetEndDate { get; set; }
    public string FileCreatedBy { get; set; } = string.Empty;
    public DateTime? DateCreated { get; set; }
    public string Comment { get; set; } = string.Empty;
}
