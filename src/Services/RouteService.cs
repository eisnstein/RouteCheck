using System.Text.RegularExpressions;

using Microsoft.OpenApi;

namespace RouteCheck.Services;

public sealed record Route(string Method, string Path);

public static class RouteService
{
    /// <summary>
    /// The HTTP methods OpenAPI knows, in the order routes with the same path are listed.
    /// </summary>
    public static readonly string[] Methods = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", "TRACE", "QUERY"];

    /// <summary>
    /// The routes of the document, sorted by path and then by method.
    /// </summary>
    public static List<Route> GetRoutes(OpenApiDocument openApiDoc)
    {
        var routes = new List<Route>();

        foreach (KeyValuePair<string, IOpenApiPathItem> path in openApiDoc.Paths)
        {
            if (path.Value.Operations is null)
            {
                continue;
            }

            foreach (HttpMethod method in path.Value.Operations.Keys)
            {
                routes.Add(new Route(method.Method.ToUpperInvariant(), path.Key));
            }
        }

        return routes
            .OrderBy(r => r.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => Array.IndexOf(Methods, r.Method) is var index and >= 0 ? index : int.MaxValue)
            .ThenBy(r => r.Method, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Keeps the routes with one of the methods (if any are given) and whose path
    /// matches one of the patterns (if any are given).
    /// </summary>
    public static List<Route> Filter(IEnumerable<Route> routes, IReadOnlyCollection<string> methods, IReadOnlyCollection<string> patterns)
    {
        var matchers = patterns.Select(CreatePathMatcher).ToList();

        return routes
            .Where(r => methods.Count == 0 || methods.Contains(r.Method, StringComparer.OrdinalIgnoreCase))
            .Where(r => matchers.Count == 0 || matchers.Any(matches => matches(r.Path)))
            .ToList();
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
