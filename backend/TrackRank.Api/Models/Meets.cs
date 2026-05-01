namespace TrackRank.Api.Models;

public class Meet
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime MeetDate { get; set; }

    public ICollection<Result> Results { get; set; } = new List<Result>();
}