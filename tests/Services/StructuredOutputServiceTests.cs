using RouteCheck.Services;

namespace RouteCheck.Tests.Services;

public class StructuredOutputServiceTests
{
    private static readonly List<Route> Routes = [new("GET", "/pets"), new("DELETE", "/pets/{id}")];

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
        return Verify(Write(output, null, [], "No project file (*.csproj) found in '/tmp/app'.")).UseParameters(output);
    }

    [Test]
    public async Task Writes_Message_When_ThereAreNoRoutes_InMarkdown()
    {
        var markdown = Write(OutputFormat.Markdown, "Api.csproj", [], null);

        await Assert.That(markdown).Contains("No routes found.");
    }

    private static string Write(string output, string? project, IReadOnlyList<Route> routes, string? error)
    {
        using var writer = new StringWriter();
        StructuredOutputService.Write(writer, output, project, routes, error);
        return writer.ToString();
    }
}
