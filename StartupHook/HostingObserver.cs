using System.Diagnostics;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RouteCheck.StartupHook;

/// <summary>
/// Waits for the "Microsoft.Extensions.Hosting" diagnostic listener, which every
/// host writes to while it is built. dotnet-getdocument and the EF Core tools use
/// the same events (HostFactoryResolver).
/// </summary>
internal sealed class HostingListenerObserver(string endpointsFile) : IObserver<DiagnosticListener>
{
    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name == "Microsoft.Extensions.Hosting")
        {
            listener.Subscribe(new HostingEventObserver(endpointsFile));
        }
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }
}

/// <summary>
/// Replaces the web server right before the host is built (after the app registered
/// its services), so the app runs as usual, but never listens on a port.
/// </summary>
internal sealed class HostingEventObserver(string endpointsFile) : IObserver<KeyValuePair<string, object?>>
{
    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (value.Key != "HostBuilding")
        {
            return;
        }

        // WebApplicationBuilder and HostApplicationBuilder pass themselves,
        // the older Host.CreateDefaultBuilder an IHostBuilder
        switch (value.Value)
        {
            case HostApplicationBuilder builder:
                Register(builder.Services);
                break;
            case IHostBuilder builder:
                builder.ConfigureServices(Register);
                break;
        }
    }

    // The last registration of IServer wins over Kestrel's
    private void Register(IServiceCollection services)
    {
        services.AddSingleton<IServer>(provider => new EndpointsServer(provider, endpointsFile));
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }
}
