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

            string path = Path.Combine(Directory.GetCurrentDirectory(), settings.Path ?? "");
            string projectFile = ProjectService.FindProjectFile(path);

            _console.MarkupLine($"Building web application [grey]{Markup.Escape(projectFile)}[/]...");

            await WebAppService.BuildProject(projectFile, cancellationToken);

            _console.MarkupLine("Starting web application and waiting to be ready...");

            var (webApp, port) = await WebAppService.StartWebApp(projectFile, TimeSpan.FromSeconds(settings.Timeout), cancellationToken);
            try
            {
                _console.MarkupLine($"Web application is running on port [grey]{port}[/]. Retrieving OpenAPI document...");

                using var httpClient = new HttpClient();
                var url = OpenApiService.BuildUrl(port, settings.OpenApiEndpoint);
                var openApiDoc = await OpenApiService.GetOpenApiDocumentAsync(httpClient, url, cancellationToken);

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
        catch (RouteCheckException ex)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}
