namespace TrackRank.Api.Models;

public class ImportHistory
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ParsedRows { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public int TrackParsedCount { get; set; }
    public int FieldParseCount { get; set; }
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
}
