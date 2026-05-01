namespace TrackRank.Api.Models;

public class Athlete
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }

    public ICollection<Result> Results { get; set; } = new List<Result>();
}