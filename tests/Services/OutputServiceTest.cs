using RouteCheck.Services;

using Spectre.Console.Testing;

namespace RouteCheck.Tests.Services;

public class OutputServiceTest
{
    [Test]
    public Task Renders_Table_Correctly()
    {
        TestConsole console = new();
        OutputService.DisplayRoutes(console, [new Route("GET", "/pets")]);

        return Verify(console.Output);
    }

    [Test]
    public Task Renders_SeveralMethods_And_EscapesMarkupInPaths()
    {
        TestConsole console = new();
        OutputService.DisplayRoutes(console,
        [
            new Route("GET", "/pets"),
            new Route("POST", "/pets"),
            new Route("DELETE", "/pets/[legacy]"),
        ]);

        return Verify(console.Output);
    }

    [Test]
    public async Task Renders_Message_When_ThereAreNoRoutes()
    {
        TestConsole console = new();
        OutputService.DisplayRoutes(console, []);

        await Assert.That(console.Output.Trim()).IsEqualTo("No routes found.");
    }
}
