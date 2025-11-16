using Microsoft.OpenApi;

using Spectre.Console;

namespace RouteCheck.Services;

public static class OutputService
{
    public static void DisplayRoutesFromSwagger(OpenApiDocument openApiDoc)
    {
        var table = new Table();
        table.Border = TableBorder.Ascii2;
        table.AddColumn("Method");
        table.AddColumn("Path");

        AnsiConsole.MarkupLine("Routes for [grey]API[/]:");
        foreach (KeyValuePair<string, IOpenApiPathItem> path in openApiDoc.Paths)
        {
            if (path.Value.Operations is null)
            {
                continue;
            }

            foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in path.Value.Operations)
            {
                var method = operation.Key.ToString();
                var methodColor = GetMethodColor(method);
                table.AddRow($"[{methodColor}]{method}[/]", path.Key);
            }
        }

        AnsiConsole.Write(table);
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