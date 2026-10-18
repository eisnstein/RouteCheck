using NuGet.Versioning;

using RouteCheck.Services;

namespace RouteCheck.Tests.Services;

public class NuGetVersionServiceTests
{
    [Test]
    public async Task GetLatestStableVersion_Returns_HighestStable_When_Unsorted()
    {
        var versions = new[] { "1.2.0", "2.0.0", "1.10.0", "2.1.0-beta.1" }
            .Select(NuGetVersion.Parse)
            .ToList();

        var latest = NuGetVersionService.GetLatestStableVersion(versions);

        await Assert.That(latest).IsEqualTo(NuGetVersion.Parse("2.0.0"));
    }

    [Test]
    public async Task GetLatestStableVersion_Returns_Null_When_OnlyPrereleases()
    {
        var versions = new[] { "1.0.0-alpha", "1.0.0-beta" }
            .Select(NuGetVersion.Parse)
            .ToList();

        var latest = NuGetVersionService.GetLatestStableVersion(versions);

        await Assert.That(latest).IsNull();
    }
}
