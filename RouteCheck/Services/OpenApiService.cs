using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.OpenApi;

public static class OpenApiService
{
    public static async Task SomeMethod()
    {
        var projectPath = @"E:\Development\Projects\MovieDB\MovieDB.Api\"; // Directory.GetCurrentDirectory();
        var port = new Random().Next(5000, 6000);

        Console.WriteLine("Attempting to fetch routes...");

        // Step 1: Try OpenAPI
        try
        {
            var process = StartWebApp(projectPath, port);
            await Task.Delay(5000); // Allow app to start
            var openApiDoc = await GetOpenApiJsonAsync(port);
            StopWebApp(process);
            if (openApiDoc is not null)
            {
                DisplayRoutesFromSwagger(openApiDoc);
                return;
            }
        }
        catch
        {
            Console.WriteLine("OpenAPI not found. Falling back to middleware injection...");
        }
    }

    private static Process StartWebApp(string projectPath, int port)
    {
        return StartProcess("dotnet", $"run --urls=http://localhost:{port}", projectPath);
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

    private static void StopWebApp(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill();
            process.WaitForExit();
        }
    }

    private static async Task<OpenApiDocument> GetOpenApiJsonAsync(int port)
    {
        // using var httpClient = new HttpClient();
        // return await httpClient.GetStringAsync($"http://localhost:{port}/openapi/v1.json");
        var (openApiDoc, _) = await OpenApiDocument.LoadAsync($"http://localhost:{port}/openapi/v1.json");
        return openApiDoc;
    }

    private static void DisplayRoutesFromSwagger(OpenApiDocument openApiDoc)
    {
        Console.WriteLine("Routes from OpenAPI:");
    }
}