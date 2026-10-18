using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RouteCheck.Tests.Commands;

public partial class CheckCommandTests
{
    // Tests building the same fixture at the same time would lock each other's files
    private const string SampleApi = "SampleApi";

    [Test]
    public async Task Returns_Error_When_ThereIsNoProject()
    {
        using var dir = TestHelper.CreateTempDirectory();
        var app = TestHelper.CreateCommandAppTester();

        var result = await app.RunAsync(["check", "--path", dir.Path]);

        await Assert.That(result.ExitCode).IsEqualTo(1);
        await Assert.That(result.Output).Contains("No project file");
    }

    [Test]
    public async Task Writes_ErrorDocument_When_ThereIsNoProject_WithJsonOutput()
    {
        using var dir = TestHelper.CreateTempDirectory();
        var app = TestHelper.CreateCommandAppTester();

        var result = await app.RunAsync(["check", "--path", dir.Path, "--output", "json"]);

        await Assert.That(result.ExitCode).IsEqualTo(1);
        using var document = JsonDocument.Parse(result.Output);
        await Assert.That(document.RootElement.GetProperty("error").GetString()).Contains("No project file");
        await Assert.That(document.RootElement.GetProperty("routes").GetArrayLength()).IsEqualTo(0);
    }

    /// <summary>
    /// Builds and starts a real web app, so it takes a few seconds.
    /// </summary>
    [Test]
    [NotInParallel(SampleApi)]
    public async Task Lists_Routes_And_StopsTheWebApp()
    {
        var app = TestHelper.CreateCommandAppTester();

        var result = await app.RunAsync(["check", "--path", TestHelper.GetFixturePath("SampleApi"), "--timeout", "60"]);

        await Assert.That(result.ExitCode).IsEqualTo(0).Because(result.Output);
        await Assert.That(result.Output).Contains("/pets/{id}");
        await Assert.That(result.Output).Contains("DELETE");

        // The web app must not keep running after RouteCheck is done
        var port = int.Parse(PortRegex().Match(result.Output).Groups[1].Value);
        using var client = new TcpClient();
        await Assert.That(() => client.ConnectAsync(IPAddress.Loopback, port)).Throws<SocketException>();
    }

    /// <summary>
    /// Builds and starts a real web app, so it takes a few seconds.
    /// </summary>
    [Test]
    [NotInParallel(SampleApi)]
    public async Task Writes_OnlyTheFilteredRoutes_AsJson()
    {
        var app = TestHelper.CreateCommandAppTester();

        var result = await app.RunAsync(
            ["check", "--path", TestHelper.GetFixturePath("SampleApi"), "--timeout", "60", "--output", "json", "-m", "get,post", "-f", "pets"]);

        await Assert.That(result.ExitCode).IsEqualTo(0).Because(result.Output);

        // Nothing but the document, e.g. no status lines
        using var document = JsonDocument.Parse(result.Output);
        var routes = document.RootElement.GetProperty("routes").EnumerateArray()
            .Select(r => $"{r.GetProperty("method").GetString()} {r.GetProperty("path").GetString()}")
            .ToList();

        await Assert.That(routes).IsEquivalentTo(["GET /pets", "POST /pets"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(document.RootElement.GetProperty("error").ValueKind).IsEqualTo(JsonValueKind.Null);
    }

    [GeneratedRegex(@"running on port (\d+)")]
    private static partial Regex PortRegex();
}
