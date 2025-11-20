using RouteCheck.Commands.Settings;
using RouteCheck.Services;

using Spectre.Console;
using Spectre.Console.Cli;

namespace RouteCheck.Commands;

public class CheckCommand : AsyncCommand<CheckSettings>
{
    private readonly IAnsiConsole _console;

    public CheckCommand(IAnsiConsole console)
    {
        _console = console;
    }

    public override async Task<int> ExecuteAsync(
        CommandContext _context,
        CheckSettings settings,
        CancellationToken _cancellationToken)
    {
        string cwd = Directory.GetCurrentDirectory();
        string pathToProject = Path.Combine(cwd, settings.Path ?? "");
        if (!File.Exists(Path.Combine(pathToProject, "Program.cs")))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] The specified path '{pathToProject}' does not appear to be a valid .NET web application project directory. Missing 'Program.cs' file.");
            return -1;
        }

        AnsiConsole.MarkupLine($"Starting web application from path: [grey]{pathToProject}[/] and waiting to be ready...");

        var (webApp, port) = await WebAppService.StartWebApp(pathToProject);

        AnsiConsole.MarkupLine($"Web application is running on port [grey]{port}[/]. Retrieving OpenAPI document...");

        var openApiDoc = await OpenApiService.GetOpenApiJsonAsync(port, settings.OpenApiEndpoint);

        Console.WriteLine();

        OutputService.DisplayRoutesFromOpenApi(_console, openApiDoc);
        WebAppService.StopWebApp(webApp);

        return 1;
    }
}