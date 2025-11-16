using RouteCheck.Commands.Settings;
using RouteCheck.Services;

using Spectre.Console.Cli;

namespace RouteCheck.Commands;

public class CheckCommand : AsyncCommand<CheckSettings>
{
    public CheckCommand()
    {
    }

    public override async Task<int> ExecuteAsync(
        CommandContext _context,
        CheckSettings _settings,
        CancellationToken _cancellationToken)
    {
        var cwd = Directory.GetCurrentDirectory();
        var (webApp, port) = await WebAppService.StartWebApp(cwd);
        var openApiDoc = await OpenApiService.GetOpenApiJsonAsync(port);

        OutputService.DisplayRoutesFromSwagger(openApiDoc);
        WebAppService.StopWebApp(webApp);

        return 1;
    }
}