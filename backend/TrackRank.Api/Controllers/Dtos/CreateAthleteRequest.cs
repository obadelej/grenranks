using System.ComponentModel.DataAnnotations;

namespace TrackRank.Api.Controllers.Dtos;

public class CreateAthleteRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    [RegularExpression("^(?i)(Male|Female|M|F)$", ErrorMessage = "Gender must be Male/Female or M/F.")]
    public string Gender { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }
}