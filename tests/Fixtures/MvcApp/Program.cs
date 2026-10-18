using Microsoft.AspNetCore.Mvc;

Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(web => web.UseStartup<Startup>())
    .Build()
    .Run();

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddRazorPages();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapRazorPages();
            endpoints.MapGet("/health", Health.Check);
        });
    }
}

public static class Health
{
    public static string Check() => "ok";
}

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    [HttpGet]
    public string[] List() => [];

    [HttpGet("{id:int}")]
    public int Get(int id) => id;

    [HttpPut("{id:int}")]
    [HttpPatch("{id:int}")]
    public int Update(int id) => id;
}
