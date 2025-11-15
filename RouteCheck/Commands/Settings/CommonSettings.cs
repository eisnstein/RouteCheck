using System.ComponentModel;
using RouteCheck.Commands.Validation;
using Spectre.Console.Cli;

namespace RouteCheck.Commands.Settings;

public class CommonSettings : CommandSettings
{
    [CommandOption("--csprojFile <Path>")]
    [Description(@"Path to *.csproj file. (default .\*.csproj)")]
    public string? PathToCsProjFile { get; set; }

    [CommandOption("--slnFile <Path>")]
    [Description(@"Path to *.sln file. (default .\*.sln)")]
    public string? PathToSlnFile { get; set; }

    [CommandOption("--slnxFile <Path>")]
    [Description(@"Path to *.slnx file. (default .\*.slnx)")]
    public string? PathToSlnxFile { get; set; }

    [CommandOption("--cpmFile <Path>")]
    [Description(@"Path to Directory.Packages.props file. (default .\Directory.Packages.props)")]
    public string? PathToCpmFile { get; set; }

    [CommandOption("--fbaFile <Path>")]
    [Description(@"Path to file-based app file which includes package directives.")]
    public string? PathToFbaFile { get; set; }

    [CommandOption("--format <Format>")]
    [Description("Format the output by the given value. Possible values: group")]
    [ValidateFormatValue("Format value has to be 'group'.")]
    public string? Format { get; set; }
}
