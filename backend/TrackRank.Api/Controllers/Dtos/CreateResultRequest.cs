using System.ComponentModel.DataAnnotations;

namespace TrackRank.Api.Controllers.Dtos;

public class CreateResultRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int AthleteId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int MeetId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Required]
    [Range(0.0001, 9999)]
    public decimal Performance { get; set; }

    [Range(-10, 10)]
    public decimal? Wind { get; set; }

    [Required]
    public DateTime ResultDate { get; set; }
}