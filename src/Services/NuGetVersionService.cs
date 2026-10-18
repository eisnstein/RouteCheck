using NuGet.Versioning;

namespace RouteCheck.Services;

public static class NuGetVersionService
{
    public static NuGetVersion? GetLatestStableVersion(IReadOnlyList<NuGetVersion> versions)
    {
        // Don't rely on the feed returning the versions in order
        return versions
            .Where(v => !v.IsPrerelease)
            .Max();
    }
}
