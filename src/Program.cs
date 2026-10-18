using System.Text;

using RouteCheck.Commands;
using RouteCheck.Services;

using Spectre.Console.Cli;

// Set console encoding to UTF-8 to properly display Unicode characters (box-drawing chars for tables)
Console.OutputEncoding = Encoding.UTF8;

// Turn Ctrl+C into a cancellation, so the started web app is stopped before exiting
using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellationTokenSource.Cancel();
};

var app = new CommandApp();

app.SetDefaultCommand<CheckCommand>();

app.Configure(
    config =>
    {
        config.SetApplicationName("routecheck");

        // Registered as a type (not an instance), so the NuGet feed is only
        // contacted when a command actually runs, e.g. not for --help.
        config.Settings.Registrar.Register<INuGetApiService, NuGetApiService>();

#if DEBUG
        config.PropagateExceptions();
        config.ValidateExamples();
#endif

        config.AddCommand<CheckCommand>("check")
            .WithAlias("c")
            .WithDescription("Check your routes. (default command)")
            .WithExample(["check", "--path", "/path/to/webapp"]);
    });

return await app.RunAsync(args, cancellationTokenSource.Token)
    .ConfigureAwait(false);
