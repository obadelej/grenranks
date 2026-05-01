using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrackRank.Api.Data;
using TrackRank.Api.Models;

namespace TrackRank.Api.Tests;

public class ImportsHistoryIntegrationTests
{
    [Fact]
    public async Task GetHistory_EmptyDatabase_ReturnsPagedResponseWithEmptyItems()
    {
        await using var factory = new WebAppFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/imports/history?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        Assert.Equal(0, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(0, doc.RootElement.GetProperty("items").GetArrayLength());
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 201)]
    public async Task GetHistory_InvalidPaging_ReturnsBadRequest(int page, int pageSize)
    {
        await using var factory = new WebAppFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/imports/history?page={page}&pageSize={pageSize}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            body.Contains("page must be greater than 0", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("pageSize must be between 1 and 200", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetHistory_AfterPostImport_ReturnsOneRow()
    {
        await using var factory = new WebAppFactory();
        var client = factory.CreateClient();

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("E;T;;1;;100M;M;;;;;10.50;;0.0;;;;;;;;;;Test;Athlete;M;01/01/2007;;Club;;;"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "File", "smoke.csv");

        var postResponse = await client.PostAsync("/api/imports/hytek", form);
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var getResponse = await client.GetAsync("/api/imports/history?page=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var doc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        var arr = doc.RootElement.GetProperty("items");
        Assert.Equal(1, arr.GetArrayLength());
        var row = arr[0];
        Assert.Equal("smoke.csv", row.GetProperty("fileName").GetString());
        Assert.Equal(0, row.GetProperty("importedCount").GetInt32());
    }

    [Fact]
    public async Task GetHistory_AfterSeedingImportHistory_ReturnsRow()
    {
        await using var factory = new WebAppFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ImportHistories.Add(new ImportHistory
            {
                FileName = "seed.csv",
                TotalRows = 10,
                ParsedRows = 10,
                ImportedCount = 3,
                SkippedCount = 7,
                ErrorCount = 0,
                TrackParsedCount = 4,
                FieldParseCount = 6,
                ImportedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/imports/history?page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = doc.RootElement.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("seed.csv", items[0].GetProperty("fileName").GetString());
        Assert.Equal(4, items[0].GetProperty("trackParsedCount").GetInt32());
        Assert.Equal(6, items[0].GetProperty("fieldParseCount").GetInt32());
    }
}
