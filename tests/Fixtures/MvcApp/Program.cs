using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;

Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(web => web.UseStartup<Startup>())
    .Build()
    .Run();

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Controllers and pages (but not minimal APIs) need a signed-in user, unless they allow anonymous users
        services.AddControllers(options => options.Filters.Add(new AuthorizeFilter()));
        services.AddRazorPages();
        services.AddAuthorization();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseAuthorization();
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
    [AllowAnonymous]
    public string[] List() => [];

    [HttpGet("{id:int}")]
    public int Get(int id) => id;

    [HttpPut("{id:int}")]
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "admin,editor")]
    public int Update(int id) => id;
}
