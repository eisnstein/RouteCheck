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
