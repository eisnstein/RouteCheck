using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

namespace RouteCheck.Commands.Settings;

public sealed class CheckSettings : CommandSettings
{
    [CommandOption("-p|--path <Path>")]
    [Description(@"Path to the web app's project file or directory. If not specified, the current directory will be used. Can be a relative or absolute path.")]
    public string? Path { get; set; }

    // --openApiEndpoint is kept for compatibility
    [CommandOption("--openapi-endpoint|--openApiEndpoint <OpenApiEndpoint>")]
    [Description(@"OpenApi endpoint. If not specified, the default '/openapi/v1.json' will be used.")]
    [DefaultValue("/openapi/v1.json")]
    public required string OpenApiEndpoint { get; set; }

    [CommandOption("--timeout <Seconds>")]
    [Description(@"Seconds to wait for the web app to start, after it has been built.")]
    [DefaultValue(20)]
    public int Timeout { get; set; }

    public override ValidationResult Validate()
    {
        return Timeout > 0
            ? ValidationResult.Success()
            : ValidationResult.Error("--timeout must be greater than 0.");
    }
}
