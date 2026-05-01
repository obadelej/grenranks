namespace TrackRank.Api.Models;

public class Result
{
    public int Id { get; set; }

    public int AthleteId { get; set; }
    public Athlete Athlete { get; set; } = null!;

    public int MeetId { get; set; }
    public Meet Meet { get; set; } = null!;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public decimal Performance { get; set; } // seconds or meters
    public decimal? Wind { get; set; }
    public DateTime ResultDate { get; set; }

    public string SourceType { get; set; } = "Manual"; // Manual or HytekImport
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}