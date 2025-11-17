using System.ComponentModel;

using Spectre.Console.Cli;

namespace RouteCheck.Commands.Settings;

public sealed class CheckSettings : CommandSettings
{
    [CommandOption("-p|--path <Path>")]
    [Description(@"Path to web app for which to check routes. If not specified, the current directory will be used. Can be a relative or absolute path.")]
    public string? Path { get; set; }

    [CommandOption("--openApiEndpoint <OpenApiEndpoint>")]
    [Description(@"OpenApi endpoint. If not specified, the default '/openapi/v1.json' will be used.")]
    [DefaultValue("/openapi/v1.json")]
    public required string OpenApiEndpoint { get; set; }
}
