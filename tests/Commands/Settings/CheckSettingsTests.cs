using RouteCheck.Commands.Settings;

namespace RouteCheck.Tests.Commands.Settings;

public class CheckSettingsTests
{
    [Test]
    [Arguments(0)]
    [Arguments(-5)]
    public async Task Validate_Fails_When_TimeoutIsNotPositive(int timeout)
    {
        var settings = new CheckSettings { OpenApiEndpoint = "/openapi/v1.json", Timeout = timeout };

        await Assert.That(settings.Validate().Successful).IsFalse();
    }

    [Test]
    public async Task Validate_Succeeds_When_TimeoutIsPositive()
    {
        var settings = new CheckSettings { OpenApiEndpoint = "/openapi/v1.json", Timeout = 20 };

        await Assert.That(settings.Validate().Successful).IsTrue();
    }
}
