using System.Diagnostics;
using System.Net.Sockets;
using System.Net;

public static class WebAppService
{
    public static async Task<(Process webApp, int port)> StartWebApp(string projectPath, int? port = null)
    {
        port ??= GetAvailablePort();
        Process process = StartProcess("dotnet", $"run --urls=http://localhost:{port}", projectPath);
        await Task.Delay(5000); // Allow app to start

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
}