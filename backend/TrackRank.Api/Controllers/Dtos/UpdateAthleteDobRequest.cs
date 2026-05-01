using System.ComponentModel.DataAnnotations;

namespace TrackRank.Api.Controllers.Dtos;

public class UpdateAthleteDobRequest
{
    [Required]
    public DateTime DateOfBirth { get; set; }
}
