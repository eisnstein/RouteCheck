using RouteCheck.Services;

namespace RouteCheck.Tests.Services;

public class BaselineServiceTests
{
    [Test]
    public async Task Compare_Finds_AddedRemovedAndChangedRoutes()
    {
        List<Route> baseline =
        [
            new("GET", "/pets", "Pets.List", "Anonymous"),
            new("DELETE", "/pets/{id}", "Pets.Delete", "Policy: Admin"),
            new("GET", "/owners", "Owners.List", "Anonymous"),
        ];
        List<Route> routes =
        [
            new("GET", "/pets", "Pets.List", "Authenticated"),
            new("GET", "/OWNERS", "Owners.List", "Anonymous"),
            new("POST", "/pets", "Pets.Create", "Policy: Admin"),
        ];

        var diff = BaselineService.Compare("routes.json", baseline, routes);

        await Assert.That(diff.Added.Select(r => $"{r.Method} {r.Path}")).IsEquivalentTo(["POST /pets"]);
        await Assert.That(diff.Removed.Select(r => $"{r.Method} {r.Path}")).IsEquivalentTo(["DELETE /pets/{id}"]);
        await Assert.That(diff.Changed.Select(BaselineService.Describe)).IsEquivalentTo(
        [
            "Auth: Anonymous → Authenticated",
            // Routing ignores the case, so it is the same route
            "Path: /owners → /OWNERS",
        ], TUnit.Assertions.Enums.CollectionOrdering.Any);
    }

    [Test]
    public async Task Compare_IgnoresAuth_When_TheBaselineHasNone()
    {
        var diff = BaselineService.Compare("routes.json", [new("GET", "/pets", "Pets.List")], [new("GET", "/pets", "Pets.List", "Anonymous")]);

        await Assert.That(diff.HasChanges).IsFalse();
    }

    [Test]
    public async Task Load_Reads_TheRoutesOfAJsonDocument()
    {
        using var dir = TestHelper.CreateTempDirectory();
        var file = Path.Combine(dir.Path, "routes.json");
        File.WriteAllText(file, """
            {
              "command": "check",
              "project": "Api.csproj",
              "routes": [{ "method": "GET", "path": "/pets", "handler": "Pets.List", "auth": "Anonymous" }],
              "problems": [],
              "error": null
            }
            """);

        var routes = BaselineService.Load(file);

        await Assert.That(routes).IsEquivalentTo([new Route("GET", "/pets", "Pets.List", "Anonymous")]);
    }

    [Test]
    [Arguments("not json", "is not a document written with --output json")]
    [Arguments("""{ "something": "else" }""", "it has no routes")]
    [Arguments("""{ "routes": [], "error": "No project file found." }""", "is the result of a failed check: No project file found.")]
    public async Task Load_Fails_For_OtherFiles(string content, string message)
    {
        using var dir = TestHelper.CreateTempDirectory();
        var file = Path.Combine(dir.Path, "routes.json");
        File.WriteAllText(file, content);

        await Assert.That(() => BaselineService.Load(file)).Throws<RouteCheckException>().WithMessageContaining(message);
    }

    [Test]
    public async Task Load_Fails_When_TheFileDoesNotExist()
    {
        await Assert.That(() => BaselineService.Load("missing-routes.json")).Throws<RouteCheckException>().WithMessageContaining("routecheck --output json > missing-routes.json");
    }
}
