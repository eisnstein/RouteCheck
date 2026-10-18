using RouteCheck.Services;
using RouteCheck.StartupHook;

namespace RouteCheck.Tests.Services;

public class RouteServiceTests
{
    private static readonly List<Route> Routes =
    [
        new("ANY", "/", "Page /Index"),
        new("GET", "/orders", "OrdersController.List"),
        new("GET", "/pets", null),
        new("POST", "/pets", null),
        new("GET", "/pets/{id}", null),
        new("DELETE", "/pets/{id}", null),
        new("GET", "/users/{id}/pets", null),
    ];

    [Test]
    public async Task GetRoutes_SortsByPath_AndThenByMethod()
    {
        List<EndpointRoute> endpoints =
        [
            new(["GET"], "/users", null),
            new(["DELETE"], "/pets/{id}", null),
            new(["OPTIONS", "PATCH"], "/pets/{id}", null),
            new(["GET", "PUT"], "/pets/{id}", null),
            new(["POST"], "/Pets", null),
            new(["GET"], "/Pets", null),
        ];

        var routes = RouteService.GetRoutes(endpoints);

        await Assert.That(routes.Select(r => $"{r.Method} {r.Path}")).IsEquivalentTo(
        [
            "GET /Pets",
            "POST /Pets",
            "GET /pets/{id}",
            "PUT /pets/{id}",
            "PATCH /pets/{id}",
            "DELETE /pets/{id}",
            "OPTIONS /pets/{id}",
            "GET /users",
        ], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task GetRoutes_Uses_AnyMethod_When_EndpointHasNoMethods()
    {
        var routes = RouteService.GetRoutes([new([], "Orders/Details/{id:int}", "Page /Orders/Details")]);

        await Assert.That(routes).IsEquivalentTo([new Route("ANY", "/Orders/Details/{id:int}", "Page /Orders/Details")]);
    }

    [Test]
    [Arguments("api/orders", "/api/orders")]
    [Arguments("/api/orders", "/api/orders")]
    [Arguments("", "/")]
    public async Task GetRoutes_StartsPaths_WithASlash(string pattern, string path)
    {
        var routes = RouteService.GetRoutes([new(["GET"], pattern, null)]);

        await Assert.That(routes.Single().Path).IsEqualTo(path);
    }

    [Test]
    public async Task GetRoutes_UppercasesMethods_And_RemovesDuplicates()
    {
        var routes = RouteService.GetRoutes([new(["get"], "/pets", "A.B"), new(["GET"], "/pets", "A.B")]);

        await Assert.That(routes).IsEquivalentTo([new Route("GET", "/pets", "A.B")]);
    }

    [Test]
    public async Task Filter_Returns_AllRoutes_Without_MethodsAndPatterns()
    {
        await Assert.That(RouteService.Filter(Routes, [], [])).IsEquivalentTo(Routes);
    }

    [Test]
    public async Task Filter_KeepsRoutes_WithOneOfTheMethods_And_RoutesForAnyMethod()
    {
        var routes = RouteService.Filter(Routes, ["post", "DELETE"], []);

        await Assert.That(routes.Select(r => $"{r.Method} {r.Path}")).IsEquivalentTo(["ANY /", "POST /pets", "DELETE /pets/{id}"]);
    }

    [Test]
    public async Task Filter_KeepsRoutes_ContainingTheText()
    {
        var routes = RouteService.Filter(Routes, [], ["PETS/"]);

        await Assert.That(routes.Select(r => $"{r.Method} {r.Path}")).IsEquivalentTo(["GET /pets/{id}", "DELETE /pets/{id}"]);
    }

    [Test]
    [Arguments("/pets*")]
    [Arguments("/pets%")]
    public async Task Filter_KeepsRoutes_MatchingTheWholeWildcardPattern(string pattern)
    {
        var routes = RouteService.Filter(Routes, [], [pattern]);

        // Not /users/{id}/pets, as the pattern has to match from the start
        await Assert.That(routes.Select(r => r.Path).Distinct()).IsEquivalentTo(["/pets", "/pets/{id}"]);
    }

    [Test]
    public async Task Filter_KeepsRoutes_MatchingAnyPattern_And_AnyMethod()
    {
        var routes = RouteService.Filter(Routes, ["GET"], ["orders", "/users/*"]);

        await Assert.That(routes.Select(r => $"{r.Method} {r.Path}")).IsEquivalentTo(["GET /orders", "GET /users/{id}/pets"]);
    }
}
