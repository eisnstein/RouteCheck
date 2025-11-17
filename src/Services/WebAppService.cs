using System.Diagnostics;
using System.Net.Sockets;
using System.Net;

public static class WebAppService
{
    public static async Task<(Process webApp, int port)> StartWebApp(string projectPath, int? port = null, TimeSpan? startupTimeout = null)
    {
        port ??= GetAvailablePort();
        Process process = StartProcess("dotnet", $"run --urls=http://localhost:{port}", projectPath);

        await WaitForWebAppReadyAsync(process, port.Value, startupTimeout ?? TimeSpan.FromSeconds(20));

        return (process, port.Value);
    }

    public static void StopWebApp(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill();
            process.WaitForExit();
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

    private static Process StartProcess(string fileName, string arguments, string workingDirectory)
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

        process.Start();

        return process;
    }

    private static async Task WaitForWebAppReadyAsync(Process process, int port, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException("The web application process exited before it started accepting requests.");
            }

            if (await IsPortOpenAsync(port))
            {
                return;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"Timed out waiting for the web application to start listening on port {port}.");
    }

    private static async Task<bool> IsPortOpenAsync(int port)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(IPAddress.Loopback, port);
            var completed = await Task.WhenAny(connectTask, Task.Delay(500));

            if (completed == connectTask)
            {
                await connectTask;
                return true;
            }
        }
        catch (SocketException)
        {
            // Port is not accepting connections yet
        }

        return false;
    }
}