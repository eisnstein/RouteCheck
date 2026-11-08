using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id:int?}");

app.Run();

public class HomeController : Controller
{
    public string Index() => "index";

    public string About() => "about";
}

public class ReportsController : Controller
{
    [HttpGet]
    public int Monthly(int id) => id;
}
