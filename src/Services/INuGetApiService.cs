using NuGet.Versioning;

namespace RouteCheck.Services;

public interface INuGetApiService
{
    Task<IEnumerable<NuGetVersion>> GetPackageVersions(string packageName, CancellationToken cancellationToken);
}
