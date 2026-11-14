var builder = WebApplication.CreateBuilder(args);

// Must not run while RouteCheck reads the routes
builder.Services.AddHostedService<MarkerWorker>();

var app = builder.Build();

// There is no launch profile, so this is only mapped because RouteCheck
// starts the app in Development
if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/info", () => "dev");
}

app.MapGet("/pets", () => Array.Empty<string>());
app.MapPost("/pets", (string name) => name);
app.MapDelete("/pets/{id}", (int id) => id);
app.MapMethods("/pets/{id}", ["PUT", "PATCH"], (int id) => id);

// Setting the address needs the server's IServerAddressesFeature
app.Run("http://localhost:5199");

/// <summary>
/// Writes the file named by SAMPLEAPI_WORKER_MARKER, so tests can tell whether it ran.
/// </summary>
internal sealed class MarkerWorker : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Environment.GetEnvironmentVariable("SAMPLEAPI_WORKER_MARKER") is { Length: > 0 } marker)
        {
            File.WriteAllText(marker, "ran");
        }

        return Task.CompletedTask;
    }
}
