using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using RouteCheck.StartupHook;

namespace RouteCheck.Services;

/// <param name="Method">The HTTP method, or <see cref="RouteService.AnyMethod"/>.</param>
/// <param name="Auth">See <see cref="EndpointRoute.Auth"/>, <c>null</c> if unknown.</param>
/// <param name="MatchKey">See <see cref="EndpointRoute.MatchKey"/>. Not part of the documents.</param>
public sealed record Route(string Method, string Path, string? Handler, string? Auth = null, [property: JsonIgnore] string? MatchKey = null)
{
    /// <summary>
    /// Whether anyone may request the route, without signing in.
    /// </summary>
    [JsonIgnore]
    public bool IsAnonymous => Auth == RouteService.Anonymous;
}

/// <summary>
/// Which routes to show. Empty lists don't filter.
/// </summary>
/// <param name="Methods">The HTTP methods, uppercase. Routes accepting any method match every method.</param>
/// <param name="Paths">Texts the path contains, or wildcard patterns it matches.</param>
/// <param name="AnonymousOnly">Only routes anyone may request.</param>
/// <param name="Handlers">Texts the handler contains, or wildcard patterns it matches.</param>
/// <param name="Excludes">Texts or wildcard patterns of paths to leave out, e.g. framework routes.</param>
public sealed record RouteFilter(
    IReadOnlyCollection<string> Methods,
    IReadOnlyCollection<string> Paths,
    bool AnonymousOnly = false,
    IReadOnlyCollection<string>? Handlers = null,
    IReadOnlyCollection<string>? Excludes = null)
{
    public static readonly RouteFilter None = new([], []);
}

/// <summary>
/// Something wrong with the routes, e.g. two routes routing can't choose between.
/// </summary>
/// <param name="Type">What kind of problem it is, e.g. <see cref="RouteService.AmbiguousProblem"/>.</param>
/// <param name="Message">The problem, in plain text.</param>
/// <param name="Routes">The routes with the problem.</param>
public sealed record RouteProblem(string Type, string Message, IReadOnlyList<Route> Routes);

public static class RouteService
{
    /// <summary>
    /// The method of routes that accept any HTTP method, e.g. Razor Pages.
    /// </summary>
    public const string AnyMethod = "ANY";

    /// <summary>
    /// The HTTP methods, in the order routes with the same path are listed.
    /// </summary>
    public static readonly string[] Methods = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", "TRACE", "QUERY"];

    /// <summary>
    /// One route per endpoint and method, sorted by path and then by method.
    /// </summary>
    public static List<Route> GetRoutes(IEnumerable<EndpointRoute> endpoints)
    {
        return endpoints
            .SelectMany(e => (e.Methods.Count == 0 ? [AnyMethod] : e.Methods)
                .Select(method => new Route(method.ToUpperInvariant(), NormalizePath(e.Pattern), e.Handler, e.Auth, e.MatchKey)))
            .Distinct()
            .OrderBy(r => r.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => Array.IndexOf(Methods, r.Method) is var index and >= 0 ? index : int.MaxValue)
            .ThenBy(r => r.Method, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// What the startup hook reports for routes anyone may request.
    /// </summary>
    public const string Anonymous = EndpointsContract.Anonymous;

    /// <summary>
    /// Keeps the routes with one of the methods (if any are given), whose path
    /// matches one of the patterns and whose handler one of the handler patterns
    /// (if any are given), and whose path matches none of the excludes. Routes
    /// accepting any method match every method.
    /// </summary>
    public static List<Route> Filter(IEnumerable<Route> routes, RouteFilter filter)
    {
        var pathMatchers = filter.Paths.Select(CreateMatcher).ToList();
        var handlerMatchers = (filter.Handlers ?? []).Select(CreateMatcher).ToList();
        var excludeMatchers = (filter.Excludes ?? []).Select(CreateMatcher).ToList();

        return routes
            .Where(r => filter.Methods.Count == 0 || r.Method == AnyMethod || filter.Methods.Contains(r.Method, StringComparer.OrdinalIgnoreCase))
            .Where(r => pathMatchers.Count == 0 || pathMatchers.Any(matches => matches(r.Path)))
            .Where(r => handlerMatchers.Count == 0 || (r.Handler is not null && handlerMatchers.Any(matches => matches(r.Handler))))
            .Where(r => !excludeMatchers.Any(matches => matches(r.Path)))
            .Where(r => !filter.AnonymousOnly || r.IsAnonymous)
            .ToList();
    }

    /// <summary>
    /// Routes with the same method that match the same requests, e.g. "/pets/{id}" and
    /// "/pets/{name}". Requesting them fails with an AmbiguousMatchException. Routes
    /// with a method take precedence over routes accepting any method, so these don't clash.
    /// </summary>
    public static List<RouteProblem> FindProblems(IEnumerable<Route> routes)
    {
        return routes
            .Where(r => r.MatchKey is not null)
            .GroupBy(r => (r.Method, r.MatchKey))
            .Where(group => group.Count() > 1)
            .Select(group => group.ToList())
            .Select(clashing => new RouteProblem(
                AmbiguousProblem,
                $"{string.Join(" and ", clashing.Select(Describe))} match the same requests, so requesting them fails with an AmbiguousMatchException. Use different paths or route constraints, e.g. {{id:int}}.",
                clashing))
            .ToList();

        static string Describe(Route route) => route.Handler is null ? $"{route.Method} {route.Path}" : $"{route.Method} {route.Path} ({route.Handler})";
    }

    /// <summary>
    /// The type of the problem of routes routing can't choose between.
    /// </summary>
    public const string AmbiguousProblem = "ambiguous-routes";

    // Controllers and Razor Pages have patterns without a leading slash, e.g. "api/orders"
    private static string NormalizePath(string pattern)
    {
        return pattern.StartsWith('/') ? pattern : "/" + pattern;
    }

    // ASP.NET Core routing is case-insensitive, so matching is too.
    // Without a wildcard, the text can be anywhere in the path or handler. With a
    // wildcard (* or %, like in PackCheck), the pattern has to match all of it.
    private static Func<string, bool> CreateMatcher(string pattern)
    {
        if (!pattern.Contains('*') && !pattern.Contains('%'))
        {
            return path => path.Contains(pattern, StringComparison.OrdinalIgnoreCase);
        }

        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("%", ".*") + "$";
        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return regex.IsMatch;
    }
}
