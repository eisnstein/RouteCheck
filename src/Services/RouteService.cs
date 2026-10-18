using System.Text.RegularExpressions;

using RouteCheck.StartupHook;

namespace RouteCheck.Services;

/// <param name="Method">The HTTP method, or <see cref="RouteService.AnyMethod"/>.</param>
public sealed record Route(string Method, string Path, string? Handler);

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
                .Select(method => new Route(method.ToUpperInvariant(), NormalizePath(e.Pattern), e.Handler)))
            .Distinct()
            .OrderBy(r => r.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => Array.IndexOf(Methods, r.Method) is var index and >= 0 ? index : int.MaxValue)
            .ThenBy(r => r.Method, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Keeps the routes with one of the methods (if any are given) and whose path
    /// matches one of the patterns (if any are given). Routes accepting any method
    /// match every method.
    /// </summary>
    public static List<Route> Filter(IEnumerable<Route> routes, IReadOnlyCollection<string> methods, IReadOnlyCollection<string> patterns)
    {
        var matchers = patterns.Select(CreatePathMatcher).ToList();

        return routes
            .Where(r => methods.Count == 0 || r.Method == AnyMethod || methods.Contains(r.Method, StringComparer.OrdinalIgnoreCase))
            .Where(r => matchers.Count == 0 || matchers.Any(matches => matches(r.Path)))
            .ToList();
    }

    // Controllers and Razor Pages have patterns without a leading slash, e.g. "api/orders"
    private static string NormalizePath(string pattern)
    {
        return pattern.StartsWith('/') ? pattern : "/" + pattern;
    }

    // ASP.NET Core routing is case-insensitive, so matching is too.
    // Without a wildcard, the text can be anywhere in the path. With a wildcard
    // (* or %, like in PackCheck), the pattern has to match the whole path.
    private static Func<string, bool> CreatePathMatcher(string pattern)
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
