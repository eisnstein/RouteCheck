using System.Collections.Immutable;
using System.Reflection;

using NuGet.Versioning;

using Spectre.Console;

namespace RouteCheck.Services;

public static class RouteCheckService
{
    public static async Task CheckForNewRouteCheckVersion(NuGetApiService nuGetApiService)
    {
        IEnumerable<NuGetVersion> result = await nuGetApiService.GetPackageVersions("RouteCheck");
        ImmutableList<NuGetVersion> versions = result.ToImmutableList();

        if (versions is { Count: 0 })
        {
            return;
        }

        NuGetVersion? latestVersion = NuGetVersionService.GetLatestStableVersion(versions);
        if (latestVersion is null)
        {
            return;
        }

        var assembly = typeof(RouteCheckService).Assembly;
        var currentVersionStr = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (currentVersionStr is null)
        {
            return;
        }

        var currentVersion = NuGetVersion.Parse(currentVersionStr);

        // If there is a newer version available, show information
        if (latestVersion > currentVersion)
        {
            AnsiConsole.MarkupLine($"[dim]INFO:[/] A new version of RouteCheck is available: {currentVersion} -> {latestVersion}");
            AnsiConsole.MarkupLine($"[dim]INFO:[/] Changelog: [link]https://github.com/eisnstein/RouteCheck/blob/main/CHANGELOG.md[/]");
            AnsiConsole.MarkupLine("[dim]INFO:[/] Run [blue]dotnet tool update --global RouteCheck[/] to update");
            Console.WriteLine();
        }
    }
}
