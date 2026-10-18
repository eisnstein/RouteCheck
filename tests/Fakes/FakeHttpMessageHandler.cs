using System.Net;

namespace RouteCheck.Tests.Fakes;

/// <summary>
/// Answers every request with the given status code and content.
/// </summary>
public class FakeHttpMessageHandler(HttpStatusCode statusCode, string content = "") : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(content) });
    }
}
