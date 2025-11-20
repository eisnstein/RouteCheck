using NuGet.Versioning;

namespace RouteCheck.Services;

public static class NuGetVersionService
{
    public static NuGetVersion? GetLatestStableVersion(IReadOnlyList<NuGetVersion> versions)
    {
        return versions
            .Where(v => v.IsPrerelease == false)
            .LastOrDefault();
    }

    public static NuGetVersion GetLatestVersion(IReadOnlyList<NuGetVersion> versions)
    {
        return versions.Last();
    }
}
