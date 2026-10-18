using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace RouteCheck.Tests.Commands;

public partial class CheckCommandTests
{
    [Test]
    public async Task Returns_Error_When_ThereIsNoProject()
    {
        using var dir = TestHelper.CreateTempDirectory();
        var app = TestHelper.CreateCommandAppTester();

        var result = await app.RunAsync(["check", "--path", dir.Path]);

        await Assert.That(result.ExitCode).IsEqualTo(1);
        await Assert.That(result.Output).Contains("No project file");
    }

    /// <summary>
    /// Builds and starts a real web app, so it takes a few seconds.
    /// </summary>
    [Test]
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

    [GeneratedRegex(@"running on port (\d+)")]
    private static partial Regex PortRegex();
}
