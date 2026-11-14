using System.ComponentModel;

using RouteCheck.Services;

using Spectre.Console;
using Spectre.Console.Cli;

namespace RouteCheck.Commands.Settings;

public sealed class CheckSettings : CommandSettings
{
    [CommandOption("-p|--path <Path>")]
    [Description(@"Path to the web app's project file or directory. If not specified, the current directory will be used. Can be a relative or absolute path.")]
    public string? Path { get; set; }

    /// <summary>
    /// No longer used, as the routes are read from the app itself instead of its
    /// OpenAPI document. Still accepted, so existing scripts keep working.
    /// </summary>
    [CommandOption("--openapi-endpoint|--openApiEndpoint <OpenApiEndpoint>", IsHidden = true)]
    public string? OpenApiEndpoint { get; set; }

    [CommandOption("--framework <Framework>")]
    [Description(@"Target framework to build and run, e.g. net9.0. Required if the web app targets multiple frameworks.")]
    public string? Framework { get; set; }

    [CommandOption("--timeout <Seconds>")]
    [Description(@"Seconds to wait for the web app to start and report its routes, after it has been built.")]
    [DefaultValue(20)]
    public int Timeout { get; set; }

    [CommandOption("-m|--method <Method>")]
    [Description(@"Show only routes with the given HTTP method, e.g. GET (can be used multiple times or comma-separated).")]
    public string[]? Method { get; set; }

    [CommandOption("-f|--filter <Pattern>")]
    [Description(@"Show only routes whose path contains the given text, or matches the given wildcard pattern (* or %) (can be used multiple times).")]
    public string[]? Filter { get; set; }

    [CommandOption("--handler <Pattern>")]
    [Description(@"Show only routes whose handler contains the given text, or matches the given wildcard pattern (* or %), e.g. Orders* (can be used multiple times).")]
    public string[]? Handler { get; set; }

    [CommandOption("-x|--exclude <Pattern>")]
    [Description(@"Leave out routes whose path contains the given text, or matches the given wildcard pattern (* or %), e.g. /_framework/* (can be used multiple times).")]
    public string[]? Exclude { get; set; }

    [CommandOption("--anonymous")]
    [Description(@"Show only routes anyone may request, without signing in.")]
    public bool Anonymous { get; set; }

    [CommandOption("--baseline <File>")]
    [Description(@"Compare the routes with a file written earlier with --output json, and show the added, removed and changed routes.")]
    public string? Baseline { get; set; }

    [CommandOption("--strict")]
    [Description(@"Exit with code 2 if there are problems with the routes, e.g. ambiguous routes, or routes of the --baseline were removed. For CI jobs.")]
    public bool Strict { get; set; }

    [CommandOption("--output <Output>")]
    [Description(@"Output of the routes: table (default), json or markdown. With json and markdown only the result is printed, e.g. for scripts and LLM agents.")]
    [DefaultValue(OutputFormat.Table)]
    public string Output { get; set; } = OutputFormat.Table;

    /// <summary>
    /// The methods of --method, uppercase and with comma-separated values split up.
    /// </summary>
    public IReadOnlyList<string> GetMethods()
    {
        return (Method ?? [])
            .SelectMany(m => m.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(m => m.ToUpperInvariant())
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Which routes to show, from --method, --filter, --handler, --exclude and --anonymous.
    /// </summary>
    public RouteFilter GetFilter() => new(GetMethods(), Filter ?? [], Anonymous, Handler ?? [], Exclude ?? []);

    public override ValidationResult Validate()
    {
        if (Timeout <= 0)
        {
            return ValidationResult.Error("--timeout must be greater than 0.");
        }

        if (!OutputFormat.All.Contains(Output))
        {
            return ValidationResult.Error($"Value '{Markup.Escape(Output)}' for --output is not valid. Valid values are: {string.Join(", ", OutputFormat.All)}");
        }

        var unknownMethods = GetMethods().Except(RouteService.Methods).ToList();
        if (unknownMethods.Count > 0)
        {
            return ValidationResult.Error($"Unknown HTTP method '{Markup.Escape(string.Join("', '", unknownMethods))}' for --method. Valid values are: {string.Join(", ", RouteService.Methods)}");
        }

        return ValidationResult.Success();
    }
}
