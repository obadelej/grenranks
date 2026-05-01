using System.ComponentModel.DataAnnotations;

namespace TrackRank.Api.Controllers.Dtos;

public class ImportHytekFileRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
}
