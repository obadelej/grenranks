using System.ComponentModel.DataAnnotations;

namespace TrackRank.Api.Controllers.Dtos;

public class CreateEventRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [RegularExpression("^(?i)(Track|Field)$", ErrorMessage = "EventType must be Track or Field.")]
    public string EventType { get; set; } = string.Empty;

}