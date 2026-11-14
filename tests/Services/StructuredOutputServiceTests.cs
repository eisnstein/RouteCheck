using RouteCheck.Services;

namespace RouteCheck.Tests.Services;

public class StructuredOutputServiceTests
{
    private static readonly List<Route> Routes = [new("GET", "/pets", "PetsController.List", "Anonymous"), new("DELETE", "/pets/{id}", null, "Policy: Admin")];

    [Test]
    [Arguments(OutputFormat.Json)]
    [Arguments(OutputFormat.Markdown)]
    public Task Writes_Routes(string output)
    {
        return Verify(Write(output, "src/Api/Api.csproj", Routes, null)).UseParameters(output);
    }

    [Test]
    [Arguments(OutputFormat.Json)]
    [Arguments(OutputFormat.Markdown)]
    public Task Writes_Error(string output)
    {
        // Not a temp path like /tmp, Verify would replace it with {TempPath} on Linux
        return Verify(Write(output, null, [], "No project file (*.csproj) found in 'src/Api'.")).UseParameters(output);
    }

    [Test]
    public async Task Writes_Message_When_ThereAreNoRoutes_InMarkdown()
    {
        var markdown = Write(OutputFormat.Markdown, "Api.csproj", [], null);

        await Assert.That(markdown).Contains("No routes found.");
    }

    [Test]
    [Arguments(OutputFormat.Json)]
    [Arguments(OutputFormat.Markdown)]
    public Task Writes_Problems(string output)
    {
        List<Route> routes = [new("GET", "/pets/{id}", "A.ById"), new("GET", "/pets/{name}", "A.ByName")];
        var problems = RouteService.FindProblems(routes.Select(r => r with { MatchKey = "pets/{}" }));

        return Verify(Write(output, new CheckResult("Api.csproj", routes, problems, null))).UseParameters(output);
    }

    private static string Write(string output, string? project, IReadOnlyList<Route> routes, string? error)
    {
        return Write(output, new CheckResult(project, routes, [], error));
    }

    private static string Write(string output, CheckResult result)
    {
        using var writer = new StringWriter();
        StructuredOutputService.Write(writer, output, result);
        return writer.ToString();
    }
}
