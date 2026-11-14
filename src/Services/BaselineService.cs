using System.Text.Json;

namespace RouteCheck.Services;

/// <summary>
/// How the routes differ from a baseline, a document written earlier with --output json.
/// </summary>
/// <param name="File">The baseline file, as given.</param>
/// <param name="Added">Routes that are not in the baseline.</param>
/// <param name="Removed">Routes of the baseline that no longer exist. Requesting them fails, so this breaks clients.</param>
/// <param name="Changed">Routes whose handler or auth changed.</param>
public sealed record BaselineDiff(string File, IReadOnlyList<Route> Added, IReadOnlyList<Route> Removed, IReadOnlyList<RouteChange> Changed)
{
    public bool HasChanges => Added.Count > 0 || Removed.Count > 0 || Changed.Count > 0;
}

/// <param name="Before">The route in the baseline.</param>
/// <param name="After">The route now.</param>
public sealed record RouteChange(Route Before, Route After);

public static class BaselineService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// The routes of a document written with --output json.
    /// </summary>
    public static List<Route> Load(string file)
    {
        if (!System.IO.File.Exists(file))
        {
            throw new RouteCheckException($"The baseline '{file}' does not exist. Write one with: routecheck --output json > {file}");
        }

        BaselineDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<BaselineDocument>(System.IO.File.ReadAllText(file), JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new RouteCheckException($"The baseline '{file}' is not a document written with --output json: {ex.Message}");
        }

        if (document?.Routes is null)
        {
            throw new RouteCheckException($"The baseline '{file}' is not a document written with --output json, it has no routes.");
        }

        if (document.Error is not null)
        {
            throw new RouteCheckException($"The baseline '{file}' is the result of a failed check: {document.Error}");
        }

        return document.Routes.Select(r => new Route(r.Method, r.Path, r.Handler, r.Auth)).ToList();
    }

    /// <summary>
    /// Compares the routes by method and path, which is what clients request.
    /// </summary>
    public static BaselineDiff Compare(string file, IReadOnlyList<Route> baseline, IReadOnlyList<Route> routes)
    {
        var before = baseline.DistinctBy(Key).ToDictionary(Key);
        var after = routes.DistinctBy(Key).ToDictionary(Key);

        return new BaselineDiff(
            file,
            routes.Where(r => !before.ContainsKey(Key(r))).ToList(),
            baseline.Where(r => !after.ContainsKey(Key(r))).ToList(),
            routes
                .Where(r => before.TryGetValue(Key(r), out var old) && (old.Path != r.Path || old.Handler != r.Handler || AuthChanged(old, r)))
                .Select(r => new RouteChange(before[Key(r)], r))
                .ToList());
    }

    // Documents of RouteCheck before 0.3.0 have no auth
    private static bool AuthChanged(Route before, Route after) => before.Auth is not null && before.Auth != after.Auth;

    // Routing ignores the case of paths
    private static (string, string) Key(Route route) => (route.Method.ToUpperInvariant(), route.Path.ToLowerInvariant());

    /// <summary>
    /// What changed about a route, e.g. "Auth: Anonymous → Policy: Admin".
    /// </summary>
    public static string Describe(RouteChange change)
    {
        var changes = new List<string>();

        if (change.Before.Path != change.After.Path)
        {
            changes.Add($"Path: {change.Before.Path} → {change.After.Path}");
        }

        if (change.Before.Handler != change.After.Handler)
        {
            changes.Add($"Handler: {change.Before.Handler ?? "none"} → {change.After.Handler ?? "none"}");
        }

        if (AuthChanged(change.Before, change.After))
        {
            changes.Add($"Auth: {change.Before.Auth} → {change.After.Auth ?? "unknown"}");
        }

        return string.Join(", ", changes);
    }

    private sealed record BaselineDocument(List<BaselineRoute>? Routes, string? Error);

    private sealed record BaselineRoute(string Method, string Path, string? Handler, string? Auth);
}
