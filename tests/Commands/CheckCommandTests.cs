using System.Text.Json;

using RouteCheck.Commands;

namespace RouteCheck.Tests.Commands;

public class CheckCommandTests
{
    // Tests building the same fixture at the same time would lock each other's files
    private const string SampleApi = "SampleApi";
    private const string MvcApp = "MvcApp";
    private const string ConventionalApp = "ConventionalApp";

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
            "GET /owners/{id:int} Program (lambda)",
            "GET /owners/{name} Program (lambda)",
            "GET /pets Program (lambda)",
            "POST /pets Program (lambda)",
            "GET /pets/{id} Program (lambda)",
            "PUT /pets/{id} Program (lambda)",
            "PATCH /pets/{id} Program (lambda)",
            "DELETE /pets/{id} Program (lambda)",
            "GET /pets/{name} Program (lambda)",
        ], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [NotInParallel(SampleApi)]
    [Arguments(false, 0)]
    [Arguments(true, CheckCommand.ProblemsExitCode)]
    public async Task Reports_AmbiguousRoutes_And_FailsWithStrict(bool strict, int exitCode)
    {
        var app = TestHelper.CreateCommandAppTester();

        string[] strictArgument = strict ? ["--strict"] : [];
        var result = await app.RunAsync(["check", "--path", TestHelper.GetFixturePath(SampleApi), "--timeout", "60", "--output", "json", .. strictArgument]);

        await Assert.That(result.ExitCode).IsEqualTo(exitCode).Because(result.Output);
        using var document = JsonDocument.Parse(result.Output);
        var problems = document.RootElement.GetProperty("problems").EnumerateArray().ToList();
        await Assert.That(problems).Count().IsEqualTo(1);
        var paths = problems[0].GetProperty("routes").EnumerateArray().Select(r => r.GetProperty("path").GetString()!).ToList();
        await Assert.That(paths).IsEquivalentTo(["/pets/{id}", "/pets/{name}"], TUnit.Assertions.Enums.CollectionOrdering.Any);
    }

    [Test]
    [NotInParallel(SampleApi)]
    public async Task DoesNotRun_HostedServices()
    {
        using var dir = TestHelper.CreateTempDirectory();
        var marker = Path.Combine(dir.Path, "worker-ran.txt");
        Environment.SetEnvironmentVariable("SAMPLEAPI_WORKER_MARKER", marker);

        try
        {
            await RunJson(SampleApi);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SAMPLEAPI_WORKER_MARKER", null);
        }

        await Assert.That(File.Exists(marker)).IsFalse();
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

    /// <summary>
    /// Builds and runs a real web app with conventional MVC routes for the given
    /// framework, so it takes a few seconds.
    /// </summary>
    [Test]
    [NotInParallel(ConventionalApp)]
    [Arguments("net8.0")]
    [Arguments("net9.0")]
    public async Task Lists_Routes_OfConventionalControllers_ForTheGivenFramework(string framework)
    {
        var routes = await RunJson(ConventionalApp, "--framework", framework);

        await Assert.That(routes).IsEquivalentTo(
        [
            "ANY /Home/About/{id:int?} HomeController.About",
            "ANY /Home/Index/{id:int?} HomeController.Index",
            "GET /Reports/Monthly/{id:int?} ReportsController.Monthly",
        ], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [NotInParallel(ConventionalApp)]
    public async Task Returns_Error_Naming_TheFrameworkOption_When_ProjectIsMultiTargeted()
    {
        var app = TestHelper.CreateCommandAppTester();

        var result = await app.RunAsync(["check", "--path", TestHelper.GetFixturePath(ConventionalApp), "--timeout", "60", "--output", "json"]);

        await Assert.That(result.ExitCode).IsEqualTo(1);
        using var document = JsonDocument.Parse(result.Output);
        await Assert.That(document.RootElement.GetProperty("error").GetString()).Contains("--framework");
    }

    [Test]
    [NotInParallel(SampleApi)]
    public async Task Writes_OnlyTheFilteredRoutes()
    {
        var routes = await RunJson(SampleApi, "-m", "get,post", "-f", "pets");

        await Assert.That(routes).IsEquivalentTo(["GET /pets Program (lambda)", "POST /pets Program (lambda)", "GET /pets/{id} Program (lambda)", "GET /pets/{name} Program (lambda)"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
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

    [Test]
    [NotInParallel(SampleApi)]
    public async Task Lists_Auth_OfMinimalApis_WithFallbackPolicy()
    {
        var routes = await RunJson(SampleApi, Auth);

        await Assert.That(routes).IsEquivalentTo(
        [
            "GET /dev/info Authenticated (fallback)",
            "GET /owners/{id:int} Authenticated (fallback)",
            "GET /owners/{name} Authenticated (fallback)",
            "GET /pets Anonymous",
            "POST /pets Claim: scope=pets:write",
            "GET /pets/{id} Authenticated (fallback)",
            "PUT /pets/{id} Authenticated (fallback)",
            "PATCH /pets/{id} Authenticated (fallback)",
            "DELETE /pets/{id} Policy: Admin",
            "GET /pets/{name} Authenticated (fallback)",
        ], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [NotInParallel(SampleApi)]
    public async Task Writes_OnlyAnonymousRoutes()
    {
        var routes = await RunJson(SampleApi, Auth, "--anonymous");

        await Assert.That(routes).IsEquivalentTo(["GET /pets Anonymous"]);
    }

    [Test]
    [NotInParallel(MvcApp)]
    public async Task Lists_Auth_OfControllers_And_RazorPages_WithGlobalAuthorizeFilter()
    {
        var routes = await RunJson(MvcApp, Auth);

        await Assert.That(routes).IsEquivalentTo(
        [
            "ANY / Authenticated",
            "GET /api/orders Anonymous",
            "GET /api/orders/{id:int} Authenticated",
            "PUT /api/orders/{id:int} Roles: admin, editor",
            "PATCH /api/orders/{id:int} Roles: admin, editor",
            // MVC filters don't apply to minimal APIs
            "GET /health Anonymous",
            "ANY /Index Authenticated",
            "ANY /Orders/Details/{id:int} Authenticated",
        ], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [NotInParallel(SampleApi)]
    public async Task Compares_Routes_WithTheBaseline_And_FailsWithStrict_When_RoutesWereRemoved()
    {
        using var dir = TestHelper.CreateTempDirectory();
        var baseline = Path.Combine(dir.Path, "routes.json");
        File.WriteAllText(baseline, """
            {
              "routes": [
                { "method": "GET", "path": "/pets", "handler": "Program (lambda)", "auth": "Policy: Admin" },
                { "method": "GET", "path": "/cats", "handler": "Program (lambda)", "auth": "Anonymous" }
              ]
            }
            """);
        var app = TestHelper.CreateCommandAppTester();

        var result = await app.RunAsync(["check", "--path", TestHelper.GetFixturePath(SampleApi), "--timeout", "60", "--output", "json", "-f", "/pets", "-f", "/cats", "--baseline", baseline, "--strict"]);

        await Assert.That(result.ExitCode).IsEqualTo(CheckCommand.ProblemsExitCode).Because(result.Output);
        using var document = JsonDocument.Parse(result.Output);
        var diff = document.RootElement.GetProperty("baseline");
        await Assert.That(diff.GetProperty("removed").EnumerateArray().Select(r => r.GetProperty("path").GetString()!)).IsEquivalentTo(["/cats"]);
        await Assert.That(diff.GetProperty("changed").EnumerateArray().Select(c => c.GetProperty("description").GetString()!)).IsEquivalentTo(["Auth: Policy: Admin → Anonymous"]);
        await Assert.That(diff.GetProperty("added").GetArrayLength()).IsGreaterThan(0);
    }

    [Test]
    [NotInParallel(SampleApi)]
    public async Task Runs_TheWebApp_InTheGivenEnvironment_And_Configuration()
    {
        var routes = await RunJson(SampleApi, "--environment", "Production", "--configuration", "Release", "-f", "/dev");

        // Only mapped in Development
        await Assert.That(routes).IsEmpty();
    }

    [Test]
    [NotInParallel(ConventionalApp)]
    [Arguments(new[] { "--launch-profile", "Staging" }, true)]
    // The environment wins over the launch profile's
    [Arguments(new[] { "--launch-profile", "Staging", "-e", "Development" }, false)]
    [Arguments(new[] { "-e", "Staging" }, true)]
    public async Task Runs_TheWebApp_WithTheGivenLaunchProfile_Or_Environment(string[] arguments, bool isStaging)
    {
        var routes = await RunJson(ConventionalApp, ["--framework", "net9.0", "-f", "/staging", .. arguments]);

        await Assert.That(routes.Count).IsEqualTo(isStaging ? 1 : 0);
    }

    [Test]
    public async Task DoesNotBuild_TheWebApp_With_NoBuild()
    {
        // A copy that was never built
        using var dir = TestHelper.CreateTempDirectory();
        foreach (var file in new[] { "SampleApi.csproj", "Program.cs" })
        {
            File.Copy(Path.Combine(TestHelper.GetFixturePath(SampleApi), file), Path.Combine(dir.Path, file));
        }

        var app = TestHelper.CreateCommandAppTester();

        var result = await app.RunAsync(["check", "--path", dir.Path, "--timeout", "60", "--no-build"]);

        await Assert.That(result.ExitCode).IsEqualTo(1);
        await Assert.That(result.Output).DoesNotContain("Building web application");
        await Assert.That(result.Output).Contains("exited before RouteCheck could read its endpoints");
    }

    [Test]
    [NotInParallel(SampleApi)]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Lists_Details_OnlyWithVerbose(bool verbose)
    {
        string[] verboseArgument = verbose ? ["--verbose"] : [];
        var routes = await RunJson(SampleApi, r => r.ToString(), ["-f", "/pets%", "-m", "get", .. verboseArgument]);

        using var route = JsonDocument.Parse(routes.First());
        string? Get(string name) => route.RootElement.TryGetProperty(name, out var value) ? value.GetString() : null;

        await Assert.That(Get("name")).IsEqualTo(verbose ? "ListPets" : null);
        await Assert.That(Get("tags")).IsEqualTo(verbose ? "Pets" : null);
        await Assert.That(Get("produces")).IsEqualTo(verbose ? "200 application/json" : null);
    }

    // "METHOD /path Handler"
    private static string Handler(JsonElement route) => $"{route.GetProperty("method").GetString()} {route.GetProperty("path").GetString()} {route.GetProperty("handler").GetString()}";

    // "METHOD /path Auth"
    private static string Auth(JsonElement route) => $"{route.GetProperty("method").GetString()} {route.GetProperty("path").GetString()} {route.GetProperty("auth").GetString()}";

    /// <summary>
    /// Runs the check with --output json and returns the routes as "METHOD /path Handler".
    /// </summary>
    private static Task<List<string>> RunJson(string fixture, params string[] arguments) => RunJson(fixture, Handler, arguments);

    /// <summary>
    /// Runs the check with --output json and returns the routes formatted with the given function.
    /// </summary>
    private static async Task<List<string>> RunJson(string fixture, Func<JsonElement, string> format, params string[] arguments)
    {
        var app = TestHelper.CreateCommandAppTester();

        var result = await app.RunAsync(["check", "--path", TestHelper.GetFixturePath(fixture), "--timeout", "60", "--output", "json", .. arguments]);

        await Assert.That(result.ExitCode).IsEqualTo(0).Because(result.Output);

        // Nothing but the document, e.g. no status lines
        using var document = JsonDocument.Parse(result.Output);
        await Assert.That(document.RootElement.GetProperty("error").ValueKind).IsEqualTo(JsonValueKind.Null);

        return document.RootElement.GetProperty("routes").EnumerateArray().Select(format).ToList();
    }
}
