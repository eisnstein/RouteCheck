using System.Text;

using RouteCheck.Commands;

using Spectre.Console.Cli;

// Set console encoding to UTF-8 to properly display Unicode characters (box-drawing chars for tables)
Console.OutputEncoding = Encoding.UTF8;

var app = new CommandApp();

app.SetDefaultCommand<CheckCommand>();

app.Configure(
    config =>
    {
        config.SetApplicationName("routecheck");

#if DEBUG
        config.PropagateExceptions();
        config.ValidateExamples();
#endif

        config.AddCommand<CheckCommand>("check")
            .WithAlias("c")
            .WithDescription("Check your routes. (default command)")
            .WithExample(["check", "--path", "/path/to/webapp"]);
    });

return await app.RunAsync(args)
    .ConfigureAwait(false);
