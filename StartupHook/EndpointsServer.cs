using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
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

    public async Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        where TContext : notnull
    {
        var endpoints = services.GetService<EndpointDataSource>()?.Endpoints ?? [];
        var auth = new AuthDescriber(services);
        var routes = new List<EndpointRoute>();

        foreach (var endpoint in endpoints.OfType<RouteEndpoint>().Where(CanReceiveRequests))
        {
            routes.Add(ToRoute(endpoint) with { Auth = await auth.DescribeAsync(endpoint) });
        }

        await File.WriteAllTextAsync(endpointsFile, JsonSerializer.Serialize(routes, EndpointsContract.JsonOptions), cancellationToken);

        // Stopping gracefully instead of throwing, as apps often report a failed
        // startup, e.g. to their error tracking
        services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
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

    // Conventional routes also add an endpoint that is only used for generating links
    private static bool CanReceiveRequests(Endpoint endpoint)
    {
        return endpoint.Metadata.GetMetadata<ISuppressMatchingMetadata>()?.SuppressMatching != true;
    }

    private static EndpointRoute ToRoute(RouteEndpoint endpoint)
    {
        return new EndpointRoute(
            endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.ToList() ?? [],
            GetPattern(endpoint),
            // The display name of e.g. SignalR hubs or health checks is the best there is
            GetHandler(endpoint) ?? endpoint.DisplayName,
            GetMatchKey(endpoint));
    }

    // Parameter names and defaults don't matter for matching: "pets/{id}" and "pets/{name}"
    // match the same requests, a parameter with a default is optional. Constraints,
    // the order, hosts and content types do matter.
    private static string GetMatchKey(RouteEndpoint endpoint)
    {
        var pattern = endpoint.RoutePattern;
        var path = string.Join('/', pattern.PathSegments.Select(segment => string.Concat(segment.Parts.Select(part => part switch
        {
            RoutePatternLiteralPart literal => literal.Content.ToLowerInvariant(),
            RoutePatternSeparatorPart separator => separator.Content,
            RoutePatternParameterPart parameter when pattern.RequiredValues.GetValueOrDefault(parameter.Name) is string { Length: > 0 } value => value.ToLowerInvariant(),
            RoutePatternParameterPart parameter => FormatMatchedParameter(parameter, pattern),
            _ => "",
        }))));

        var hosts = endpoint.Metadata.GetMetadata<IHostMetadata>()?.Hosts ?? [];
        var contentTypes = endpoint.Metadata.GetMetadata<IAcceptsMetadata>()?.ContentTypes ?? [];

        return $"{path} order={endpoint.Order} hosts={JoinSorted(hosts)} accepts={JoinSorted(contentTypes)}";
    }

    private static string FormatMatchedParameter(RoutePatternParameterPart parameter, RoutePattern pattern)
    {
        // Constraints can also be given outside the template, e.g. in MapControllerRoute
        var policies = parameter.ParameterPolicies
            .Concat(pattern.ParameterPolicies.GetValueOrDefault(parameter.Name) ?? [])
            .Select(p => p.Content ?? p.ParameterPolicy?.GetType().Name)
            .Where(p => p is not null)
            // The pattern's policies include the inline ones
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase);
        var catchAll = parameter.IsCatchAll ? "*" : "";
        var optional = parameter.IsOptional || parameter.Default is not null || pattern.Defaults.ContainsKey(parameter.Name) ? "?" : "";

        return $"{{{catchAll}{string.Concat(policies.Select(p => ":" + p))}{optional}}}";
    }

    private static string JoinSorted(IEnumerable<string> values)
    {
        return string.Join(',', values.Select(v => v.ToLowerInvariant()).Order(StringComparer.Ordinal));
    }

    // The raw text of a conventional route is the template all its actions share, e.g.
    // "{controller=Home}/{action=Index}/{id?}". The controller and action of this
    // endpoint are its required values, so they are filled in: "Home/About/{id?}".
    private static string GetPattern(RouteEndpoint endpoint)
    {
        var pattern = endpoint.RoutePattern;

        return endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is { AttributeRouteInfo: null }
            ? string.Join('/', pattern.PathSegments.Select(segment => string.Concat(segment.Parts.Select(part => FormatPart(part, pattern.RequiredValues)))))
            : pattern.RawText ?? "";
    }

    private static string FormatPart(RoutePatternPart part, IReadOnlyDictionary<string, object?> requiredValues)
    {
        return part switch
        {
            RoutePatternLiteralPart literal => literal.Content,
            RoutePatternSeparatorPart separator => separator.Content,
            RoutePatternParameterPart parameter when requiredValues.GetValueOrDefault(parameter.Name) is string { Length: > 0 } value => value,
            RoutePatternParameterPart parameter => FormatParameter(parameter),
            _ => "",
        };
    }

    private static string FormatParameter(RoutePatternParameterPart parameter)
    {
        var catchAll = !parameter.IsCatchAll ? "" : parameter.EncodeSlashes ? "*" : "**";
        var policies = string.Concat(parameter.ParameterPolicies.Where(p => p.Content is not null).Select(p => ":" + p.Content));
        var defaultValue = parameter.Default is null ? "" : "=" + parameter.Default;
        var optional = parameter.IsOptional ? "?" : "";

        return $"{{{catchAll}{parameter.Name}{policies}{defaultValue}{optional}}}";
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
