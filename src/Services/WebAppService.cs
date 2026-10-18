using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;

public static class WebAppService
{
    // Number of output lines of the web app kept to explain a failed startup
    private const int MaxOutputLines = 30;

    public static async Task<(Process webApp, int port)> StartWebApp(
        string projectPath,
        CancellationToken cancellationToken,
        int? port = null,
        TimeSpan? startupTimeout = null)
    {
        port ??= GetAvailablePort();
        var output = new ConcurrentQueue<string>();
        Process process = StartProcess("dotnet", $"run --urls=http://localhost:{port}", projectPath, output);

        try
        {
            await WaitForWebAppReadyAsync(process, port.Value, startupTimeout ?? TimeSpan.FromSeconds(20), output, cancellationToken);
        }
        catch
        {
            StopWebApp(process);
            throw;
        }

        return (process, port.Value);
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

    private static Process StartProcess(string fileName, string arguments, string workingDirectory, ConcurrentQueue<string> output)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            }
        };

        // The redirected output has to be read continuously, otherwise the web app
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
                throw new InvalidOperationException(
                    $"The web application process exited before it started accepting requests. Output:{Environment.NewLine}{string.Join(Environment.NewLine, output)}");
            }

            if (await IsPortOpenAsync(port, cancellationToken))
            {
                return;
            }

            await Task.Delay(250, cancellationToken);
        }

        throw new TimeoutException($"Timed out waiting for the web application to start listening on port {port}.");
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
