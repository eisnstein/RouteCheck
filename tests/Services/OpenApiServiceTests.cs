using System.Net;

using RouteCheck.Services;
using RouteCheck.Tests.Fakes;

namespace RouteCheck.Tests.Services;

public class OpenApiServiceTests
{
    private static readonly Uri Url = OpenApiService.BuildUrl(5000, "/openapi/v1.json");

    private const string ValidDocument = """
        {
          "openapi": "3.1.1",
          "info": { "title": "Sample API", "version": "1.0.0" },
          "paths": {
            "/pets": { "get": { "responses": { "200": { "description": "OK" } } } }
          }
        }
        """;

    [Test]
    public async Task BuildUrl_Returns_LocalhostUrl()
    {
        await Assert.That(Url.ToString()).IsEqualTo("http://localhost:5000/openapi/v1.json");
    }

    [Test]
    public async Task GetOpenApiDocumentAsync_Returns_TheDocument()
    {
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, ValidDocument));

        var document = await OpenApiService.GetOpenApiDocumentAsync(httpClient, Url, CancellationToken.None);

        await Assert.That(document.Paths.Keys).Contains("/pets");
    }

    [Test]
    public async Task GetOpenApiDocumentAsync_Explains_When_EndpointIsNotFound()
    {
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.NotFound));

        await Assert.That(() => (Task)OpenApiService.GetOpenApiDocumentAsync(httpClient, Url, CancellationToken.None))
            .Throws<RouteCheckException>()
            .WithMessageContaining("app.MapOpenApi()");
    }

    [Test]
    public async Task GetOpenApiDocumentAsync_Throws_When_RequestFails()
    {
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.InternalServerError));

        await Assert.That(() => (Task)OpenApiService.GetOpenApiDocumentAsync(httpClient, Url, CancellationToken.None))
            .Throws<RouteCheckException>()
            .WithMessageContaining("500");
    }

    [Test]
    public async Task GetOpenApiDocumentAsync_Throws_When_ResponseIsNoOpenApiDocument()
    {
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, "<html>Hello</html>"));

        await Assert.That(() => (Task)OpenApiService.GetOpenApiDocumentAsync(httpClient, Url, CancellationToken.None))
            .Throws<RouteCheckException>()
            .WithMessageContaining("not a valid OpenAPI document");
    }
}
