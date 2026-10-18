var builder = WebApplication.CreateBuilder(args);

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
