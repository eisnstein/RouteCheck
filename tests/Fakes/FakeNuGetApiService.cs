using NuGet.Versioning;

using RouteCheck.Services;

namespace RouteCheck.Tests.Fakes;

/// <summary>
/// Knows no versions, so command tests neither query nuget.org nor show an update notice.
/// </summary>
public class FakeNuGetApiService : INuGetApiService
{
    public Task<IEnumerable<NuGetVersion>> GetPackageVersions(string packageName, CancellationToken cancellationToken)
    {
        return Task.FromResult<IEnumerable<NuGetVersion>>([]);
    }
}
