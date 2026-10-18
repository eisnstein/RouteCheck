using NuGet.Versioning;

using RouteCheck.Services;

namespace RouteCheck.Tests.Fakes;

/// <summary>
/// Behaves like a NuGet feed that never answers and ignores cancellation.
/// </summary>
public class HangingNuGetApiService : INuGetApiService
{
    public Task<IEnumerable<NuGetVersion>> GetPackageVersions(string packageName, CancellationToken cancellationToken)
    {
        return new TaskCompletionSource<IEnumerable<NuGetVersion>>().Task;
    }
}
