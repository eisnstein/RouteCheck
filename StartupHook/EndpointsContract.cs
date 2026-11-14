using System.Text.Json;

namespace RouteCheck.StartupHook;

/// <summary>
/// What the startup hook and RouteCheck agree on. This file is compiled into both,
/// so they can't get out of sync.
/// </summary>
public static class EndpointsContract
{
    /// <summary>
    /// The file the hook writes the endpoints to. Without it, the hook does nothing.
    /// </summary>
    public const string EndpointsFileVariable = "ROUTECHECK_ENDPOINTS_FILE";

    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}

/// <param name="Methods">The HTTP methods, empty when the endpoint accepts any method (e.g. Razor Pages).</param>
/// <param name="Pattern">The route pattern as written in the app, e.g. "api/orders/{id:int}".</param>
/// <param name="Handler">What handles the request, e.g. "OrdersController.Get", if known.</param>
/// <param name="MatchKey">
/// What routing matches requests by, besides the method: the pattern without parameter
/// names, the order, hosts and accepted content types. Endpoints with the same match key
/// and method match the same requests, so routing can't choose between them.
/// </param>
public sealed record EndpointRoute(IReadOnlyList<string> Methods, string Pattern, string? Handler, string? MatchKey = null);
