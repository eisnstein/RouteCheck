using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

using RouteCheck.StartupHook;

namespace RouteCheck.Services;

public static class WebAppService
{
    // Number of output lines kept to explain a failed build or run
    private const int MaxOutputLines = 30;

    private static readonly string StartupHookPath = Path.Combine(AppContext.BaseDirectory, "hook", "RouteCheck.StartupHook.dll");

    /// <summary>
    /// Builds the project before running it, so the timeout does not include the build.
    /// </summary>
    public static async Task BuildProject(string projectFile, CancellationToken cancellationToken)
    {
        var output = new ConcurrentQueue<string>();
        using Process process = StartProcess(["build", projectFile, "--nologo", "--verbosity", "quiet"], projectFile, output, new Dictionary<string, string>());

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

        var output = new ConcurrentQueue<string>();
        using Process process = StartProcess(["run", "--project", projectFile, "--no-build"], projectFile, output, environment);

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

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }
}
