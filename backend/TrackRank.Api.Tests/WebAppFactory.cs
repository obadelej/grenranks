using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TrackRank.Api.Data;
using TrackRank.Api.Services.Import;

namespace TrackRank.Api.Tests;

/// <summary>
/// Hosts the API with an in-memory database and a fake Hy-Tek parser for integration tests.
/// Each factory instance uses a unique database name so tests stay isolated.
/// </summary>
public class WebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _environmentName;
    private readonly string? _adminApiKey;
    private readonly string _dbName = Guid.NewGuid().ToString("N");

    public WebAppFactory(string environmentName = "Testing", string? adminApiKey = null)
    {
        _environmentName = environmentName;
        _adminApiKey = adminApiKey;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            if (!string.IsNullOrWhiteSpace(_adminApiKey))
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:AdminApiKey"] = _adminApiKey
                });
            }
        });

        builder.ConfigureServices(services =>
        {
            RemoveAppDbContext(services);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.RemoveAll<IHytekCsvParser>();
            services.AddSingleton<IHytekCsvParser, FakeEmptyHytekCsvParser>();
        });
    }

    private static void RemoveAppDbContext(IServiceCollection services)
    {
        var descriptors = services.Where(d =>
            d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
            d.ServiceType == typeof(AppDbContext)).ToList();

        foreach (var d in descriptors)
            services.Remove(d);
    }
}

internal sealed class FakeEmptyHytekCsvParser : IHytekCsvParser
{
    public Task<HytekImportParseResult> ParseAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HytekImportParseResult
        {
            TotalRows = 0,
            ParsedRows = 0,
            Errors = new List<string>(),
            Rows = new List<HytekImportRow>()
        });
    }
}
