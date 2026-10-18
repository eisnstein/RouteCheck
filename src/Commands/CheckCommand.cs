using RouteCheck.Commands.Settings;
using RouteCheck.Services;

using Spectre.Console;
using Spectre.Console.Cli;

namespace RouteCheck.Commands;

/// <summary>
/// Builds the web app, reads its endpoints by starting it with RouteCheck's startup hook
/// and lists the routes.
/// With --output json/markdown, nothing but one document with the result is
/// printed, at the end of the run or on an error.
/// </summary>
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
        bool isStructuredOutput = OutputFormat.IsStructured(settings.Output);
        string? project = null;

        try
        {
            // Would not be part of the document
            if (!isStructuredOutput)
            {
                await RouteCheckService.CheckForNewRouteCheckVersion(_nuGetApiService, _console, cancellationToken);
            }

            string path = Path.Combine(Directory.GetCurrentDirectory(), settings.Path ?? "");
            string projectFile = ProjectService.FindProjectFile(path);
            // With forward slashes, so the documents are the same on every OS
            project = Path.GetRelativePath(Directory.GetCurrentDirectory(), projectFile).Replace('\\', '/');

            if (settings.OpenApiEndpoint is not null)
            {
                Status("[dim]INFO:[/] --openapi-endpoint is no longer needed and is ignored, as the routes are read from the app itself.");
            }

            Status($"Building web application [grey]{Markup.Escape(projectFile)}[/]...");

            await WebAppService.BuildProject(projectFile, cancellationToken);

            Status("Starting web application to read its routes...");

            var endpoints = await WebAppService.ReadEndpoints(projectFile, TimeSpan.FromSeconds(settings.Timeout), cancellationToken);
            var routes = RouteService.Filter(RouteService.GetRoutes(endpoints), settings.GetMethods(), settings.Filter ?? []);

            if (isStructuredOutput)
            {
                StructuredOutputService.Write(_console.Profile.Out.Writer, settings.Output, project, routes, null);
            }
            else
            {
                _console.WriteLine();
                OutputService.DisplayRoutes(_console, routes);
            }

            return 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Fail("Cancelled.", "[yellow]Cancelled.[/]");
        }
        catch (RouteCheckException ex)
        {
            return Fail(ex.Message, $"[red]Error:[/] {Markup.Escape(ex.Message)}");
        }

        // Prints a status line (markup), unless the output is a JSON/Markdown document
        void Status(string markup)
        {
            if (!isStructuredOutput)
            {
                _console.MarkupLine(markup);
            }
        }

        // The error goes into the document, or is printed as the line (markup) with the table output
        int Fail(string error, string line)
        {
            if (isStructuredOutput)
            {
                StructuredOutputService.Write(_console.Profile.Out.Writer, settings.Output, project, [], error);
            }
            else
            {
                _console.MarkupLine(line);
            }

            return 1;
        }
    }
}
