using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;

namespace RouteCheck.Services;

public static class WebAppService
{
    // Number of output lines kept to explain a failed build or startup
    private const int MaxOutputLines = 30;

    /// <summary>
    /// Builds the project before starting it, so the startup timeout does not include the build.
    /// </summary>
    public static async Task BuildProject(string projectFile, CancellationToken cancellationToken)
    {
        var output = new ConcurrentQueue<string>();
        using Process process = StartProcess(["build", projectFile, "--nologo", "--verbosity", "quiet"], projectFile, output);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        if (process.ExitCode != 0)
        {
            // MSBuild repeats the errors in its summary, so only show each error once
            var errors = output.Where(line => line.Contains(": error ")).Distinct().ToList();
            var details = errors.Count > 0 ? errors : output.ToList();
            throw new RouteCheckException($"Building the web application failed:{Environment.NewLine}{string.Join(Environment.NewLine, details)}");
        }
    }

    public static async Task<(Process webApp, int port)> StartWebApp(
        string projectFile,
        TimeSpan startupTimeout,
        CancellationToken cancellationToken)
    {
        int port = GetAvailablePort();
        var output = new ConcurrentQueue<string>();
        Process process = StartProcess(
            ["run", "--project", projectFile, "--no-build", $"--urls=http://localhost:{port}"],
            projectFile,
            output);

        try
        {
            await WaitForWebAppReadyAsync(process, port, startupTimeout, output, cancellationToken);
        }
        catch
        {
            StopWebApp(process);
            throw;
        }

        return (process, port);
    }

    public static void StopWebApp(Process process)
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
        finally
        {
            process.Dispose();
        }
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static Process StartProcess(string[] arguments, string projectFile, ConcurrentQueue<string> output)
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

        // The OpenAPI endpoint is usually only mapped in Development. A launch profile
        // or an environment set by the user still takes precedence.
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") is null
            && Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") is null)
        {
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
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

    private static async Task WaitForWebAppReadyAsync(
        Process process,
        int port,
        TimeSpan timeout,
        ConcurrentQueue<string> output,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (process.HasExited)
            {
                // Wait until the remaining output has been read
                process.WaitForExit();
                throw new RouteCheckException(
                    $"The web application exited before it started accepting requests. Output:{Environment.NewLine}{string.Join(Environment.NewLine, output)}");
            }

            if (await IsPortOpenAsync(port, cancellationToken))
            {
                return;
            }

            await Task.Delay(250, cancellationToken);
        }

        throw new RouteCheckException(
            $"Timed out after {timeout.TotalSeconds:0} seconds waiting for the web application to listen on port {port}. Use --timeout to wait longer. Output:{Environment.NewLine}{string.Join(Environment.NewLine, output)}");
    }

    private static async Task<bool> IsPortOpenAsync(int port, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMilliseconds(500));

            await client.ConnectAsync(IPAddress.Loopback, port, timeout.Token);
            return true;
        }
        catch (SocketException)
        {
            // Port is not accepting connections yet
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Connecting took too long, try again
        }

        return false;
    }
}
