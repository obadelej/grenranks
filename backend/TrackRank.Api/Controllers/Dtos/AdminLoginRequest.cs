using System.ComponentModel.DataAnnotations;

namespace TrackRank.Api.Controllers.Dtos;

public class AdminLoginRequest
{
    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
