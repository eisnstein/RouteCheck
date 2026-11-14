using Spectre.Console;

namespace RouteCheck.Services;

/// <summary>
/// A column of the routes table.
/// </summary>
/// <param name="GetValue">The value of a route (plain text).</param>
public sealed record RouteColumn(string Header, Func<Route, string?> GetValue);

public static class OutputService
{
    /// <summary>
    /// The columns of the routes table, in the console and in Markdown.
    /// </summary>
    /// <param name="verbose">Adds the columns of --verbose.</param>
    public static List<RouteColumn> GetColumns(IReadOnlyList<Route> routes, bool verbose)
    {
        List<RouteColumn> columns =
        [
            new("Method", r => r.Method),
            new("Path", r => r.Path),
            new("Handler", r => r.Handler),
        ];

        if (ShowAuth(routes))
        {
            columns.Add(new("Auth", r => r.Auth));
        }

        if (verbose)
        {
            columns.Add(new("Name", r => r.Name));
            columns.Add(new("Tags", r => r.Tags));
            columns.Add(new("Accepts", r => r.Accepts));
            columns.Add(new("Produces", r => r.Produces));
        }

        return columns;
    }

    public static void DisplayRoutes(IAnsiConsole console, IReadOnlyList<Route> routes, bool verbose = false)
    {
        if (routes.Count == 0)
        {
            console.MarkupLine("[yellow]No routes found.[/]");
            return;
        }

        var columns = GetColumns(routes, verbose);

        var table = new Table();
        table.Border = TableBorder.None;
        foreach (var column in columns)
        {
            table.AddColumn(column.Header);
        }

        foreach (Route route in routes)
        {
            table.AddRow(columns.Select(column => Format(column, route)).ToArray());
        }

        console.Write(table);
    }

    private static string Format(RouteColumn column, Route route)
    {
        var value = Markup.Escape(column.GetValue(route) ?? "");

        return column.Header switch
        {
            "Method" => $"[{GetMethodColor(route.Method)}]{value}[/]",
            "Path" => value,
            "Auth" when route.IsAnonymous => $"[yellow]{value}[/]",
            "Auth" => value,
            _ => $"[grey]{value}[/]",
        };
    }

    /// <summary>
    /// Whether the routes need an Auth column: not if the app doesn't use authorization,
    /// i.e. anyone may request every route.
    /// </summary>
    public static bool ShowAuth(IEnumerable<Route> routes)
    {
        return routes.Any(r => r.Auth is not null && !r.IsAnonymous);
    }

    public static void DisplayProblems(IAnsiConsole console, IReadOnlyList<RouteProblem> problems)
    {
        if (problems.Count == 0)
        {
            return;
        }

        console.WriteLine();
        console.MarkupLine(problems.Count == 1 ? "[yellow]1 problem:[/]" : $"[yellow]{problems.Count} problems:[/]");

        foreach (var problem in problems)
        {
            console.MarkupLine($"[yellow]-[/] {Markup.Escape(problem.Message)}");
        }
    }

    public static void DisplayBaseline(IAnsiConsole console, BaselineDiff diff)
    {
        console.WriteLine();

        var file = Markup.Escape(diff.File);
        if (!diff.HasChanges)
        {
            console.MarkupLine($"[green]No changes[/] compared with [grey]{file}[/].");
            return;
        }

        console.MarkupLine($"Compared with [grey]{file}[/]: [green]{diff.Added.Count} added[/], [red]{diff.Removed.Count} removed[/], [yellow]{diff.Changed.Count} changed[/]");

        foreach (var route in diff.Added)
        {
            console.MarkupLine($"[green]+[/] {route.Method} {Markup.Escape(route.Path)}");
        }

        foreach (var route in diff.Removed)
        {
            console.MarkupLine($"[red]-[/] {route.Method} {Markup.Escape(route.Path)}");
        }

        foreach (var change in diff.Changed)
        {
            console.MarkupLine($"[yellow]~[/] {change.After.Method} {Markup.Escape(change.After.Path)} [grey]{Markup.Escape(BaselineService.Describe(change))}[/]");
        }
    }

    private static string GetMethodColor(string method)
        => method switch
        {
            "GET" => "green",
            "POST" => "blue",
            "PUT" => "yellow",
            "DELETE" => "red",
            _ => "grey"
        };
}
