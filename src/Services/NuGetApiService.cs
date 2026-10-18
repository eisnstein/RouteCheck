using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace RouteCheck.Services;

public class NuGetApiService : INuGetApiService
{
    private readonly ILogger _logger = NullLogger.Instance;
    private readonly SourceCacheContext _cache = new();
    private readonly Lazy<Task<FindPackageByIdResource>> _resource;

    public NuGetApiService()
    {
        // Getting a resource downloads the feed's service index, so it is done lazily
        // on first use. Otherwise creating this service would already fail when offline.
        var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        _resource = new(() => GetResourceAsync<FindPackageByIdResource>(repository));
    }

    private static async Task<T> GetResourceAsync<T>(SourceRepository repository) where T : class, INuGetResource
    {
        return await repository.GetResourceAsync<T>(CancellationToken.None)
            ?? throw new InvalidOperationException($"The NuGet feed does not provide a {typeof(T).Name}.");
    }

    public async Task<IEnumerable<NuGetVersion>> GetPackageVersions(string packageName, CancellationToken cancellationToken)
    {
        var resource = await _resource.Value;
        return await resource.GetAllVersionsAsync(packageName, _cache, _logger, cancellationToken);
    }
}
