using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

using RouteCheck.StartupHook;

namespace RouteCheck.Services;

/// <summary>
/// How the web app is built and run.
/// </summary>
/// <param name="Framework">The target framework, e.g. net9.0. Required for a multi-targeted project.</param>
/// <param name="Configuration">The build configuration, e.g. Release. Debug if not given.</param>
/// <param name="Environment">The environment the app runs in, e.g. Staging. Development if not given.</param>
/// <param name="LaunchProfile">The launch profile of launchSettings.json. The first one if not given.</param>
public sealed record RunOptions(string? Framework = null, string? Configuration = null, string? Environment = null, string? LaunchProfile = null)
{
    public static readonly RunOptions Default = new();
}

public static class WebAppService
{
    // Number of output lines kept to explain a failed build or run
    private const int MaxOutputLines = 30;

    private static readonly string StartupHookPath = Path.Combine(AppContext.BaseDirectory, "hook", "RouteCheck.StartupHook.dll");

    /// <summary>
    /// Builds the project before running it, so the timeout does not include the build.
    /// </summary>
    public static async Task BuildProject(string projectFile, RunOptions options, CancellationToken cancellationToken)
    {
        var output = new ConcurrentQueue<string>();
        using Process process = StartProcess(["build", projectFile, "--nologo", "--verbosity", "quiet", .. BuildArguments(options)], projectFile, output, new Dictionary<string, string>());

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        finally
        {
            Kill(process);
        }

        if (process.ExitCode != 0)
        {
            // MSBuild repeats the errors in its summary, so only show each error once
            var errors = output.Where(line => line.Contains(": error ")).Distinct().ToList();
            var details = errors.Count > 0 ? errors : output.ToList();
            throw new RouteCheckException($"Building the web application failed:{Environment.NewLine}{string.Join(Environment.NewLine, details)}");
        }
    }

    /// <summary>
    /// Runs the (built) web app with RouteCheck's startup hook, which writes the app's
    /// endpoints to a file and stops the app again. The app never listens on a port.
    /// </summary>
    public static async Task<IReadOnlyList<EndpointRoute>> ReadEndpoints(
        string projectFile,
        RunOptions options,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(StartupHookPath))
        {
            throw new RouteCheckException($"RouteCheck's startup hook is missing at '{StartupHookPath}'. Try reinstalling RouteCheck.");
        }

        var endpointsFile = Path.Combine(Path.GetTempPath(), $"routecheck-{Guid.NewGuid():N}.json");
        var environment = new Dictionary<string, string>
        {
            ["DOTNET_STARTUP_HOOKS"] = AppendStartupHook(Environment.GetEnvironmentVariable("DOTNET_STARTUP_HOOKS")),
            [EndpointsContract.EndpointsFileVariable] = endpointsFile,
        };

        string[] appArguments = [];
        if (options.Environment is not null)
        {
            environment["ASPNETCORE_ENVIRONMENT"] = options.Environment;
            environment["DOTNET_ENVIRONMENT"] = options.Environment;

            // A launch profile would override the variables, but not the command line
            // that WebApplication.CreateBuilder(args) and Host.CreateDefaultBuilder(args) read
            appArguments = ["--", "--environment", options.Environment];
        }

        string[] launchProfileArguments = options.LaunchProfile is null ? [] : ["--launch-profile", options.LaunchProfile];

        var output = new ConcurrentQueue<string>();
        using Process process = StartProcess(["run", "--project", projectFile, "--no-build", .. BuildArguments(options), .. launchProfileArguments, .. appArguments], projectFile, output, environment);

        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);

            try
            {
                await process.WaitForExitAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new RouteCheckException(
                    $"Timed out after {timeout.TotalSeconds:0} seconds waiting for the web application to start. Use --timeout to wait longer. RouteCheck supports ASP.NET Core apps on .NET 8 or later. Output:{Environment.NewLine}{string.Join(Environment.NewLine, output)}");
            }

            // The app may exit with an error code, e.g. when it does more after app.Run().
            // What counts is that the hook wrote the endpoints.
            if (!File.Exists(endpointsFile))
            {
                throw new RouteCheckException(
                    $"The web application exited before RouteCheck could read its endpoints. RouteCheck supports ASP.NET Core apps on .NET 8 or later. Output:{Environment.NewLine}{string.Join(Environment.NewLine, output)}");
            }

            await using var stream = File.OpenRead(endpointsFile);
            return await JsonSerializer.DeserializeAsync<List<EndpointRoute>>(stream, EndpointsContract.JsonOptions, cancellationToken) ?? [];
        }
        finally
        {
            Kill(process);
            File.Delete(endpointsFile);
        }
    }

    // Run has to use the output of the same build
    private static string[] BuildArguments(RunOptions options)
    {
        return
        [
            .. options.Framework is null ? [] : new[] { "--framework", options.Framework },
            .. options.Configuration is null ? [] : new[] { "--configuration", options.Configuration },
        ];
    }

    // Keeps startup hooks of the app's environment, e.g. of an APM agent
    private static string AppendStartupHook(string? existingHooks)
    {
        return string.IsNullOrEmpty(existingHooks)
            ? StartupHookPath
            : $"{existingHooks}{Path.PathSeparator}{StartupHookPath}";
    }

    private static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                // `dotnet run` starts the web app as a child process, so the whole tree has to be killed
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
            }
        }
        catch (InvalidOperationException)
        {
            // The process exited in the meantime
        }
    }

    private static Process StartProcess(
        string[] arguments,
        string projectFile,
        ConcurrentQueue<string> output,
        Dictionary<string, string> environment)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            // The content root of the web app is the working directory
            WorkingDirectory = Path.GetDirectoryName(projectFile),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // Endpoints can depend on the environment, and developers usually look at their
        // app in Development. A launch profile or an environment set by the user still
        // takes precedence.
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") is null
            && Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") is null)
        {
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        }

        foreach (var (name, value) in environment)
        {
            startInfo.Environment[name] = value;
        }

        var process = new Process { StartInfo = startInfo };

        // The redirected output has to be read continuously, otherwise the process
        // blocks as soon as the pipe buffer is full.
        void Collect(object _, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            output.Enqueue(e.Data);
            while (output.Count > MaxOutputLines)
            {
                output.TryDequeue(out string? _);
            }
        }

        process.OutputDataReceived += Collect;
        process.ErrorDataReceived += Collect;

        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            process.Dispose();
            throw new RouteCheckException($"Could not start 'dotnet': {ex.Message}. Make sure the .NET SDK is installed and on the PATH.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }
}
