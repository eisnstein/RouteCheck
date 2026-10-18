using System.Diagnostics;

using RouteCheck.StartupHook;

/// <summary>
/// Entry point for DOTNET_STARTUP_HOOKS: runs in the web app's process before its Main method.
/// Does nothing unless RouteCheck set the endpoints file.
/// </summary>
/// <remarks>
/// The runtime looks for exactly this name: a class StartupHook without namespace
/// and a static Initialize method.
/// </remarks>
internal class StartupHook
{
    public static void Initialize()
    {
        var endpointsFile = Environment.GetEnvironmentVariable(EndpointsContract.EndpointsFileVariable);
        if (string.IsNullOrEmpty(endpointsFile))
        {
            return;
        }

        DiagnosticListener.AllListeners.Subscribe(new HostingListenerObserver(endpointsFile));
    }
}
