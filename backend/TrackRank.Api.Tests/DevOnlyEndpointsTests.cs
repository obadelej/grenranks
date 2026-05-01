using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TrackRank.Api.Tests;

public class DevOnlyEndpointsTests
{
    [Fact]
    public async Task Seed_Returns403_InProduction()
    {
        await using var factory = new WebAppFactory("Production");
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/seed", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains("Development", doc.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ImportHytek_Returns403_InProduction()
    {
        await using var factory = new WebAppFactory("Production");
        var client = factory.CreateClient();

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("x"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "File", "x.csv");

        var response = await client.PostAsync("/api/imports/hytek", form);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains("Development", doc.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ImportHistory_Get_Allowed_InProduction()
    {
        await using var factory = new WebAppFactory("Production");
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/imports/history?take=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
