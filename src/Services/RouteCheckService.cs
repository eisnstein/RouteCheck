using System.Collections.Immutable;
using System.Reflection;

using NuGet.Versioning;

using Spectre.Console;

namespace RouteCheck.Services;

public static class RouteCheckService
{
    private static readonly TimeSpan VersionCheckTimeout = TimeSpan.FromSeconds(3);

    public static async Task CheckForNewRouteCheckVersion(
        INuGetApiService nuGetApiService,
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        IEnumerable<NuGetVersion> result;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(VersionCheckTimeout);
            result = await nuGetApiService
                .GetPackageVersions("RouteCheck", timeout.Token)
                .WaitAsync(timeout.Token);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // This check is optional, so it must never stop the actual command,
            // e.g. when offline, behind a proxy or when the feed is slow.
            return;
        }

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
        if (!NuGetVersion.TryParse(currentVersionStr, out var currentVersion))
        {
            return;
        }

        // If there is a newer version available, show information
        if (latestVersion > currentVersion)
        {
            console.MarkupLine($"[dim]INFO:[/] A new version of RouteCheck is available: {currentVersion} -> {latestVersion}");
            console.MarkupLine($"[dim]INFO:[/] Changelog: [link]https://github.com/eisnstein/RouteCheck/blob/main/CHANGELOG.md[/]");
            console.MarkupLine("[dim]INFO:[/] Run [blue]dotnet tool update --global RouteCheck[/] to update");
            console.WriteLine();
        }
    }
}
