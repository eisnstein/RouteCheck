var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

// Like the templates. There is no launch profile, so this only works
// because RouteCheck starts the app in Development.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/pets", () => Array.Empty<string>());
app.MapPost("/pets", (string name) => name);
app.MapDelete("/pets/{id}", (int id) => id);

app.Run();
