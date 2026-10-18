namespace RouteCheck;

/// <summary>
/// An error that is shown to the user as is, without a stack trace.
/// </summary>
public class RouteCheckException(string message) : Exception(message);
