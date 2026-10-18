using Microsoft.OpenApi;

using RouteCheck.Services;

namespace RouteCheck.Tests.Services;

public class RouteServiceTests
{
    private static readonly List<Route> Routes =
    [
        new("GET", "/orders"),
        new("GET", "/pets"),
        new("POST", "/pets"),
        new("GET", "/pets/{id}"),
        new("DELETE", "/pets/{id}"),
        new("GET", "/users/{id}/pets"),
    ];

    [Test]
    public async Task GetRoutes_SortsByPath_AndThenByMethod()
    {
        var operations = (params HttpMethod[] methods) => new OpenApiPathItem
        {
            Operations = methods.ToDictionary(m => m, _ => new OpenApiOperation())
        };
        var openApiDocument = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/users"] = operations(HttpMethod.Get),
                ["/pets/{id}"] = operations(HttpMethod.Delete, HttpMethod.Options, HttpMethod.Patch, HttpMethod.Get, HttpMethod.Put),
                ["/Pets"] = operations(HttpMethod.Post, HttpMethod.Get),
            }
        };

        var routes = RouteService.GetRoutes(openApiDocument);

        await Assert.That(routes).IsEquivalentTo(new List<Route>
        {
            new("GET", "/Pets"),
            new("POST", "/Pets"),
            new("GET", "/pets/{id}"),
            new("PUT", "/pets/{id}"),
            new("PATCH", "/pets/{id}"),
            new("DELETE", "/pets/{id}"),
            new("OPTIONS", "/pets/{id}"),
            new("GET", "/users"),
        }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task Filter_Returns_AllRoutes_Without_MethodsAndPatterns()
    {
        await Assert.That(RouteService.Filter(Routes, [], [])).IsEquivalentTo(Routes);
    }

    [Test]
    public async Task Filter_KeepsRoutes_WithOneOfTheMethods()
    {
        var routes = RouteService.Filter(Routes, ["post", "DELETE"], []);

        await Assert.That(routes).IsEquivalentTo(new List<Route> { new("POST", "/pets"), new("DELETE", "/pets/{id}") });
    }

    [Test]
    public async Task Filter_KeepsRoutes_ContainingTheText()
    {
        var routes = RouteService.Filter(Routes, [], ["PETS/"]);

        await Assert.That(routes).IsEquivalentTo(new List<Route> { new("GET", "/pets/{id}"), new("DELETE", "/pets/{id}") });
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

        await Assert.That(routes).IsEquivalentTo(new List<Route> { new("GET", "/orders"), new("GET", "/users/{id}/pets") });
    }
}
