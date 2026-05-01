using System.Text;
using TrackRank.Api.Services.Import;

namespace TrackRank.Api.Tests;

public class HytekCsvParserTests
{
    [Fact]
    public async Task ParseAsync_TrackMarkWithMinutes_ConvertsToTotalSeconds()
    {
        var parser = new HytekCsvParser();
        var csv = string.Join('\n',
            "H;National Championship 2024;03/02/2024;03/03/2024;Hy-Tek;04/13/2024;Results",
            BuildERecord("T", "800M", "M", "1:09.42", "0.0", "Doe", "John", "M", "01/01/2007", "Team Alpha"));

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await parser.ParseAsync(stream);

        Assert.Empty(result.Errors);
        Assert.Single(result.Rows);
        Assert.Equal("Track", result.Rows[0].EventType);
        Assert.Equal(69.42m, result.Rows[0].Performance);
    }

    [Fact]
    public async Task ParseAsync_IgnoredMarks_AreSkippedWithoutErrors()
    {
        var parser = new HytekCsvParser();
        var csv = string.Join('\n',
            "H;National Championship 2024;03/02/2024;03/03/2024;Hy-Tek;04/13/2024;Results",
            BuildERecord("T", "400M", "M", "DNS", "0.0", "Doe", "John", "M", "01/01/2007", "Team Alpha"),
            BuildERecord("T", "800M", "M", "2:01.50", "0.0", "Smith", "Jane", "F", "02/02/2008", "Team Beta"),
            BuildERecord("F", "LJ", "F", "FOUL", "-0.3", "Brown", "Ana", "F", "03/03/2009", "Team Gamma"));

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await parser.ParseAsync(stream);

        Assert.Empty(result.Errors);
        Assert.Single(result.Rows);
        Assert.Equal("Track", result.Rows[0].EventType);
        Assert.Equal(121.50m, result.Rows[0].Performance);
        Assert.Equal("Smith", result.Rows[0].AthleteLastName);
    }

    private static string BuildERecord(
        string eventTypeCode,
        string eventCode,
        string eventGender,
        string resultMark,
        string wind,
        string lastName,
        string firstName,
        string athleteGender,
        string birthDate,
        string teamName)
    {
        var fields = Enumerable.Repeat(string.Empty, 33).ToArray();
        fields[0] = "E";              // 1
        fields[1] = eventTypeCode;    // 2
        fields[4] = eventCode;        // 5
        fields[5] = eventGender;      // 6
        fields[10] = resultMark;      // 11
        fields[12] = wind;            // 13
        fields[22] = lastName;        // 23
        fields[23] = firstName;       // 24
        fields[25] = athleteGender;   // 26
        fields[26] = birthDate;       // 27
        fields[28] = teamName;        // 29
        return string.Join(';', fields);
    }
}
