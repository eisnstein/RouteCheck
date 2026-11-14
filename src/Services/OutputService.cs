using Spectre.Console;

namespace RouteCheck.Services;

public static class OutputService
{
    public static void DisplayRoutes(IAnsiConsole console, IReadOnlyList<Route> routes)
    {
        if (routes.Count == 0)
        {
            console.MarkupLine("[yellow]No routes found.[/]");
            return;
        }

        var showAuth = ShowAuth(routes);

        var table = new Table();
        table.Border = TableBorder.None;
        table.AddColumn("Method");
        table.AddColumn("Path");
        table.AddColumn("Handler");
        if (showAuth)
        {
            table.AddColumn("Auth");
        }

        foreach (Route route in routes)
        {
            var methodColor = GetMethodColor(route.Method);
            List<string> cells =
            [
                $"[{methodColor}]{route.Method}[/]",
                Markup.Escape(route.Path),
                $"[grey]{Markup.Escape(route.Handler ?? "")}[/]",
            ];

            if (showAuth)
            {
                cells.Add(route.IsAnonymous ? $"[yellow]{RouteService.Anonymous}[/]" : Markup.Escape(route.Auth ?? ""));
            }

            table.AddRow(cells.ToArray());
        }

        console.Write(table);
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
