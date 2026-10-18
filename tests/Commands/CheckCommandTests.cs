using System.Text.Json;

namespace RouteCheck.Tests.Commands;

public class CheckCommandTests
{
    // Tests building the same fixture at the same time would lock each other's files
    private const string SampleApi = "SampleApi";
    private const string MvcApp = "MvcApp";

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
    /// Builds and runs a real web app (minimal APIs), so it takes a few seconds.
    /// </summary>
    [Test]
    [NotInParallel(SampleApi)]
    public async Task Lists_Routes_OfMinimalApis()
    {
        var routes = await RunJson(SampleApi);

        await Assert.That(routes).IsEquivalentTo(
        [
            // Only mapped in Development
            "GET /dev/info Program (lambda)",
            "GET /pets Program (lambda)",
            "POST /pets Program (lambda)",
            "PUT /pets/{id} Program (lambda)",
            "PATCH /pets/{id} Program (lambda)",
            "DELETE /pets/{id} Program (lambda)",
        ], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    /// <summary>
    /// Builds and runs a real web app (.NET 8, Startup class, controllers and Razor Pages),
    /// so it takes a few seconds.
    /// </summary>
    [Test]
    [NotInParallel(MvcApp)]
    public async Task Lists_Routes_OfControllers_And_RazorPages()
    {
        var routes = await RunJson(MvcApp);

        await Assert.That(routes).IsEquivalentTo(
        [
            "ANY / Page /Index",
            "GET /api/orders OrdersController.List",
            "GET /api/orders/{id:int} OrdersController.Get",
            "PUT /api/orders/{id:int} OrdersController.Update",
            "PATCH /api/orders/{id:int} OrdersController.Update",
            "GET /health Health.Check",
            "ANY /Index Page /Index",
            "ANY /Orders/Details/{id:int} Page /Orders/Details",
        ], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [NotInParallel(SampleApi)]
    public async Task Writes_OnlyTheFilteredRoutes()
    {
        var routes = await RunJson(SampleApi, "-m", "get,post", "-f", "pets");

        await Assert.That(routes).IsEquivalentTo(["GET /pets Program (lambda)", "POST /pets Program (lambda)"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [NotInParallel(SampleApi)]
    public async Task Lists_Routes_AsTable_And_Ignores_OpenApiEndpoint()
    {
        var app = TestHelper.CreateCommandAppTester();

        var result = await app.RunAsync(["check", "--path", TestHelper.GetFixturePath(SampleApi), "--timeout", "60", "--openApiEndpoint", "/swagger.json"]);

        await Assert.That(result.ExitCode).IsEqualTo(0).Because(result.Output);
        await Assert.That(result.Output).Contains("--openapi-endpoint is no longer needed");
        await Assert.That(result.Output).Contains("/pets/{id}");
        await Assert.That(result.Output).Contains("Program (lambda)");
    }

    /// <summary>
    /// Runs the check with --output json and returns the routes as "METHOD /path Handler".
    /// </summary>
    private static async Task<List<string>> RunJson(string fixture, params string[] arguments)
    {
        var app = TestHelper.CreateCommandAppTester();

        var result = await app.RunAsync(["check", "--path", TestHelper.GetFixturePath(fixture), "--timeout", "60", "--output", "json", .. arguments]);

        await Assert.That(result.ExitCode).IsEqualTo(0).Because(result.Output);

        // Nothing but the document, e.g. no status lines
        using var document = JsonDocument.Parse(result.Output);
        await Assert.That(document.RootElement.GetProperty("error").ValueKind).IsEqualTo(JsonValueKind.Null);

        return document.RootElement.GetProperty("routes").EnumerateArray()
            .Select(r => $"{r.GetProperty("method").GetString()} {r.GetProperty("path").GetString()} {r.GetProperty("handler").GetString()}")
            .ToList();
    }
}
