using Microsoft.OpenApi;

public static class OpenApiService
{
    public static async Task<OpenApiDocument> GetOpenApiJsonAsync(int port, string endpoint = "openapi/v1.json")
    {
        var url = await BuildUrlAsync(port, endpoint);

        var (openApiDoc, _) = await OpenApiDocument.LoadAsync(url);
        if (openApiDoc is null)
        {
            throw new Exception("Failed to load OpenAPI document.");
        }

        return openApiDoc;
    }

    private static async Task<string> BuildUrlAsync(int port, string endpoint)
    {
        var url = new UriBuilder
        {
            Port = port,
            Path = endpoint
        }.Uri;

        return url.ToString();
    }
}