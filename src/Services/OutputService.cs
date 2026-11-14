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

        var table = new Table();
        table.Border = TableBorder.None;
        table.AddColumn("Method");
        table.AddColumn("Path");
        table.AddColumn("Handler");

        foreach (Route route in routes)
        {
            var methodColor = GetMethodColor(route.Method);
            table.AddRow(
                $"[{methodColor}]{route.Method}[/]",
                Markup.Escape(route.Path),
                $"[grey]{Markup.Escape(route.Handler ?? "")}[/]");
        }

        console.Write(table);
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
