using System.Runtime.CompilerServices;

using RouteCheck.Commands;
using RouteCheck.Services;
using RouteCheck.Tests.Fakes;

using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;

namespace RouteCheck.Tests;

public static class TestHelper
{
    public static CommandAppTester CreateCommandAppTester()
    {
        var registrar = new TestTypeRegistrar();
        registrar.RegisterInstance(typeof(INuGetApiService), new FakeNuGetApiService());

        // Wide enough that paths in the output are not wrapped
        var console = new TestConsole().Width(1000);

        var app = new CommandAppTester(registrar, console: console);
        app.SetDefaultCommand<CheckCommand>();
        app.Configure(config => config.AddCommand<CheckCommand>("check"));

        return app;
    }

    /// <summary>
    /// The directory of a web app in tests/Fixtures, found via the location of this source file.
    /// </summary>
    public static string GetFixturePath(string name)
    {
        return Path.Combine(Path.GetDirectoryName(GetThisFilePath())!, "Fixtures", name);
    }

    // CallerFilePath is the file of the caller, so it has to be called from here
    private static string GetThisFilePath([CallerFilePath] string path = "") => path;

    /// <summary>
    /// A new empty directory, deleted again when disposed.
    /// </summary>
    public static TempDirectory CreateTempDirectory() => new();

    public sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("routecheck-tests-").FullName;

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
