using RouteCheck.Services;
using RouteCheck.Tests.Fakes;

using Spectre.Console.Testing;

namespace RouteCheck.Tests.Services;

public class RouteCheckServiceTests
{
    [Test]
    public async Task CheckForNewRouteCheckVersion_DoesNotThrow_When_FeedIsUnreachable()
    {
        var nuGetApiService = new UnreachableNuGetApiService();

        await Assert.That(() => RouteCheckService.CheckForNewRouteCheckVersion(nuGetApiService, new TestConsole(), CancellationToken.None)).ThrowsNothing();
    }

    [Test]
    public async Task CheckForNewRouteCheckVersion_GivesUp_When_FeedDoesNotAnswer()
    {
        var nuGetApiService = new HangingNuGetApiService();

        var check = RouteCheckService.CheckForNewRouteCheckVersion(nuGetApiService, new TestConsole(), CancellationToken.None);
        var finished = await Task.WhenAny(check, Task.Delay(TimeSpan.FromSeconds(10)));

        await Assert.That(finished).IsEqualTo(check);
    }
}
