using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RouteCheck.StartupHook;

/// <summary>
/// Takes the place of Kestrel. When the host starts the server, the app has mapped
/// all its endpoints and built its request pipeline, so they are written to the
/// file and the app is stopped. Nothing ever listens on a port.
/// </summary>
internal sealed class EndpointsServer(IServiceProvider services, string endpointsFile) : IServer
{
    public IFeatureCollection Features { get; } = CreateFeatures();

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        where TContext : notnull
    {
        var endpoints = services.GetService<EndpointDataSource>()?.Endpoints ?? [];
        var routes = endpoints.OfType<RouteEndpoint>().Select(ToRoute).ToList();

        File.WriteAllText(endpointsFile, JsonSerializer.Serialize(routes, EndpointsContract.JsonOptions));

        // Stopping gracefully instead of throwing, as apps often report a failed
        // startup, e.g. to their error tracking
        services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Dispose()
    {
    }

    private static FeatureCollection CreateFeatures()
    {
        // Apps can set their addresses with app.Urls or app.Run(url), which throw without this feature
        var features = new FeatureCollection();
        features.Set<IServerAddressesFeature>(new ServerAddressesFeature());
        return features;
    }

    private static EndpointRoute ToRoute(RouteEndpoint endpoint)
    {
        return new EndpointRoute(
            endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.ToList() ?? [],
            endpoint.RoutePattern.RawText ?? "",
            // The display name of e.g. SignalR hubs or health checks is the best there is
            GetHandler(endpoint) ?? endpoint.DisplayName);
    }

    private static string? GetHandler(Endpoint endpoint)
    {
        if (endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is { } action)
        {
            return $"{action.ControllerTypeInfo.Name}.{action.MethodInfo.Name}";
        }

        if (endpoint.Metadata.GetMetadata<PageActionDescriptor>() is { } page)
        {
            return $"Page {page.ViewEnginePath}";
        }

        // Minimal APIs: the method passed to MapGet etc.
        if (endpoint.Metadata.GetMetadata<MethodInfo>() is { } method)
        {
            return IsLambda(method)
                ? $"{GetOuterType(method.DeclaringType)?.Name} (lambda)"
                : $"{method.DeclaringType?.Name}.{method.Name}";
        }

        return null;
    }

    private static bool IsLambda(MethodInfo method)
    {
        return method.Name.Contains('<') || method.DeclaringType?.IsDefined(typeof(CompilerGeneratedAttribute)) == true;
    }

    // Lambdas live in compiler-generated nested types like Program+<>c
    private static Type? GetOuterType(Type? type)
    {
        while (type?.IsDefined(typeof(CompilerGeneratedAttribute)) == true && type.DeclaringType is not null)
        {
            type = type.DeclaringType;
        }

        return type;
    }

    private sealed class ServerAddressesFeature : IServerAddressesFeature
    {
        public ICollection<string> Addresses { get; } = [];
        public bool PreferHostingUrls { get; set; }
    }
}
