using RouteCheck.Commands.Settings;
using RouteCheck.Services;

using Spectre.Console;
using Spectre.Console.Cli;

namespace RouteCheck.Commands;

public class CheckCommand : AsyncCommand<CheckSettings>
{
    private readonly IAnsiConsole _console;
    private readonly INuGetApiService _nuGetApiService;

    public CheckCommand(IAnsiConsole console, INuGetApiService nuGetApiService)
    {
        _console = console;
        _nuGetApiService = nuGetApiService;
    }

    public override async Task<int> ExecuteAsync(
        CommandContext _context,
        CheckSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            await RouteCheckService.CheckForNewRouteCheckVersion(_nuGetApiService, _console, cancellationToken);

            string cwd = Directory.GetCurrentDirectory();
            string pathToProject = Path.Combine(cwd, settings.Path ?? "");
            if (!File.Exists(Path.Combine(pathToProject, "Program.cs")))
            {
                _console.MarkupLine($"[red]Error:[/] The specified path '{Markup.Escape(pathToProject)}' does not appear to be a valid .NET web application project directory. Missing 'Program.cs' file.");
                return 1;
            }

            _console.MarkupLine($"Starting web application from path: [grey]{Markup.Escape(pathToProject)}[/] and waiting to be ready...");

            var (webApp, port) = await WebAppService.StartWebApp(pathToProject, cancellationToken);
            try
            {
                _console.MarkupLine($"Web application is running on port [grey]{port}[/]. Retrieving OpenAPI document...");

                var openApiDoc = await OpenApiService.GetOpenApiJsonAsync(port, settings.OpenApiEndpoint, cancellationToken);

                _console.WriteLine();

                OutputService.DisplayRoutesFromOpenApi(_console, openApiDoc);
            }
            finally
            {
                WebAppService.StopWebApp(webApp);
            }

            return 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _console.MarkupLine("[yellow]Cancelled.[/]");
            return 1;
        }
        catch (Exception ex) when (ex is InvalidOperationException or TimeoutException)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}
