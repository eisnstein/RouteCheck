using System.ComponentModel;

using Spectre.Console.Cli;

namespace RouteCheck.Commands.Settings;

public sealed class CheckSettings : CommandSettings
{
    [CommandOption("--csprojFile <Path>")]
    [Description(@"Path to *.csproj file. (default .\*.csproj)")]
    public string? PathToCsProjFile { get; set; }
}
