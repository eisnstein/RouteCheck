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

    private void Register(IServiceCollection services)
    {
        RemoveHostedServices(services);

        // The last registration of IServer wins over Kestrel's
        services.AddSingleton<IServer>(provider => new EndpointsServer(provider, endpointsFile));
    }

    // Hosted services would really do their work, e.g. consume a queue or send emails,
    // although the app only runs to report its endpoints. Only the web host's own
    // service is kept, as it builds the request pipeline and starts the server.
    private static void RemoveHostedServices(IServiceCollection services)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            var service = services[i];
            if (service.ServiceType == typeof(IHostedService) && !IsWebHostService(service))
            {
                services.RemoveAt(i);
            }
        }
    }

    private static bool IsWebHostService(ServiceDescriptor service)
    {
        var implementationType = service.IsKeyedService ? service.KeyedImplementationType : service.ImplementationType;
        return implementationType?.FullName == WebHostServiceTypeName;
    }

    private const string WebHostServiceTypeName = "Microsoft.AspNetCore.Hosting.GenericWebHostService";

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }
}
