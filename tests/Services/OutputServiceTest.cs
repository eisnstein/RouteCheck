using RouteCheck.Services;

using Spectre.Console.Testing;

namespace RouteCheck.Tests.Services;

public class OutputServiceTest
{
    [Test]
    public Task Renders_Table_Correctly()
    {
        TestConsole console = new();
        OutputService.DisplayRoutes(console, [new Route("GET", "/pets", "PetsController.List")]);

        return Verify(console.Output);
    }

    [Test]
    public Task Renders_SeveralMethods_And_EscapesMarkup()
    {
        TestConsole console = new();
        OutputService.DisplayRoutes(console,
        [
            new Route("ANY", "/", "Page /Index"),
            new Route("GET", "/pets", "Program (lambda)"),
            new Route("POST", "/pets", null),
            new Route("DELETE", "/pets/[legacy]", "[Legacy] handler"),
        ]);

        return Verify(console.Output);
    }

    [Test]
    public Task Renders_AuthColumn_When_SomeRoutesNeedAuthorization()
    {
        TestConsole console = new();
        OutputService.DisplayRoutes(console,
        [
            new Route("GET", "/pets", "Program (lambda)", "Anonymous"),
            new Route("DELETE", "/pets/{id}", "Program (lambda)", "Policy: Admin"),
        ]);

        return Verify(console.Output);
    }

    [Test]
    public Task Renders_Details_When_Verbose()
    {
        var console = new TestConsole().Width(200);
        OutputService.DisplayRoutes(console,
        [
            new Route("GET", "/pets", "Program (lambda)", "Anonymous", Name: "ListPets", Tags: "Pets", Produces: "200 application/json"),
            new Route("POST", "/pets", "Program (lambda)", "Anonymous", Accepts: "application/json", Produces: "201; 400 application/problem+json"),
        ], verbose: true);

        return Verify(console.Output);
    }

    [Test]
    public async Task Renders_NoAuthColumn_When_AllRoutesAreAnonymous()
    {
        TestConsole console = new();
        OutputService.DisplayRoutes(console, [new Route("GET", "/pets", "Program (lambda)", "Anonymous")]);

        await Assert.That(console.Output).DoesNotContain("Auth");
    }

    [Test]
    public async Task Renders_Message_When_ThereAreNoRoutes()
    {
        TestConsole console = new();
        OutputService.DisplayRoutes(console, []);

        await Assert.That(console.Output.Trim()).IsEqualTo("No routes found.");
    }
}
