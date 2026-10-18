using RouteCheck.Commands.Settings;

namespace RouteCheck.Tests.Commands.Settings;

public class CheckSettingsTests
{
    private static CheckSettings CreateSettings() => new() { Timeout = 20 };

    [Test]
    public async Task Validate_Succeeds_With_Defaults()
    {
        await Assert.That(CreateSettings().Validate().Successful).IsTrue();
    }

    [Test]
    [Arguments(0)]
    [Arguments(-5)]
    public async Task Validate_Fails_When_TimeoutIsNotPositive(int timeout)
    {
        var settings = CreateSettings();
        settings.Timeout = timeout;

        await Assert.That(settings.Validate().Successful).IsFalse();
    }

    [Test]
    public async Task Validate_Fails_When_OutputIsUnknown()
    {
        var settings = CreateSettings();
        settings.Output = "xml";

        await Assert.That(settings.Validate().Successful).IsFalse();
    }

    [Test]
    public async Task Validate_Fails_When_MethodIsUnknown()
    {
        var settings = CreateSettings();
        settings.Method = ["GET", "FETCH"];

        var result = settings.Validate();

        await Assert.That(result.Successful).IsFalse();
        await Assert.That(result.Message).Contains("FETCH");
    }

    [Test]
    public async Task GetMethods_SplitsCommaSeparatedValues_And_Uppercases()
    {
        var settings = CreateSettings();
        settings.Method = ["get, post", "Get", "delete"];

        await Assert.That(settings.GetMethods()).IsEquivalentTo(["GET", "POST", "DELETE"]);
    }
}
