using System.Net;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace RouteCheck.Services;

public static class OpenApiService
{
    public static Uri BuildUrl(int port, string endpoint)
    {
        return new UriBuilder(Uri.UriSchemeHttp, "localhost", port, endpoint).Uri;
    }

    public static async Task<OpenApiDocument> GetOpenApiDocumentAsync(HttpClient httpClient, Uri url, CancellationToken cancellationToken)
    {
        string content;
        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new RouteCheckException(
                    $"No OpenAPI document found at {url}. Make sure the web app calls builder.Services.AddOpenApi() and app.MapOpenApi(). For a custom endpoint, use --openapi-endpoint.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new RouteCheckException(
                    $"Requesting the OpenAPI document at {url} failed with {(int)response.StatusCode} {response.ReasonPhrase}.");
            }

            content = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new RouteCheckException($"Requesting the OpenAPI document at {url} failed: {ex.Message}");
        }

        ReadResult result;
        try
        {
            result = OpenApiDocument.Parse(content);
        }
        catch (Exception ex)
        {
            // E.g. NotSupportedException when the content is neither JSON nor YAML
            throw new RouteCheckException($"The response from {url} is not a valid OpenAPI document. {ex.Message}");
        }

        if (result.Document is null)
        {
            var errors = result.Diagnostic?.Errors.Select(e => e.Message) ?? [];
            throw new RouteCheckException(
                $"The response from {url} is not a valid OpenAPI document. {string.Join(" ", errors)}".TrimEnd());
        }

        return result.Document;
    }
}
