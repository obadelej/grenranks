namespace TrackRank.Api.Models;

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;

    public ICollection<Result> Results { get; set; } = new List<Result>();
}