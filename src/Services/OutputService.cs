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
