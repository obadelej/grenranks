using System.ComponentModel.DataAnnotations;

namespace TrackRank.Api.Controllers.Dtos;

public class CreateMeetRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateTime MeetDate { get; set; }
}