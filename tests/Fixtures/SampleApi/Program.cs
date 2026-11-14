var builder = WebApplication.CreateBuilder(args);

// Must not run while RouteCheck reads the routes
builder.Services.AddHostedService<MarkerWorker>();

// Routes without authorization metadata need a signed-in user
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy("Admin", policy => policy.RequireRole("admin"));
});

var app = builder.Build();

app.UseAuthorization();

// There is no launch profile, so this is only mapped because RouteCheck
// starts the app in Development
if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/info", () => "dev");
}

app.MapGet("/pets", () => Array.Empty<string>()).AllowAnonymous();
app.MapPost("/pets", (string name) => name).RequireAuthorization(policy => policy.RequireClaim("scope", "pets:write"));
app.MapDelete("/pets/{id}", (int id) => id).RequireAuthorization("Admin");

// Ambiguous: routing can't choose between them, unlike between the two below.
// The analyzer only sees this as both are mapped here, not e.g. in different files.
#pragma warning disable ASP0022
app.MapGet("/pets/{id}", (int id) => id);
app.MapGet("/pets/{name}", (string name) => name);
#pragma warning restore ASP0022
app.MapGet("/owners/{id:int}", (int id) => id);
app.MapGet("/owners/{name}", (string name) => name);
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
