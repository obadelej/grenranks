using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using TrackRank.Api.Controllers.Dtos;

namespace TrackRank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _configuration;

    public AuthController(IHostEnvironment env, IConfiguration configuration)
    {
        _env = env;
        _configuration = configuration;
    }

    [HttpPost("admin-login")]
    public IActionResult AdminLogin([FromBody] AdminLoginRequest request)
    {
        var provided = request.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(provided))
            return Unauthorized(new { message = "Admin key is required." });

        var configuredKey = _configuration["Security:AdminApiKey"]?.Trim();
        if (!string.IsNullOrWhiteSpace(configuredKey))
        {
            if (!string.Equals(provided, configuredKey, StringComparison.Ordinal))
                return Unauthorized(new { message = "Invalid admin key." });

            return Ok(new { isAdmin = true });
        }

        if (IsDevelopmentOrTestingEnvironment() &&
            string.Equals(provided, "dev-admin", StringComparison.Ordinal))
        {
            return Ok(new
            {
                isAdmin = true,
                warning = "Using development fallback key. Set Security:AdminApiKey for stricter auth."
            });
        }

        return Unauthorized(new
        {
            message = "Admin key is not configured. Set Security:AdminApiKey, or use 'dev-admin' in Development/Testing."
        });
    }

    private bool IsDevelopmentOrTestingEnvironment() =>
        _env.IsDevelopment() ||
        string.Equals(_env.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);
}
