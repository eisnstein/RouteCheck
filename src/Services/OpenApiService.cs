using Microsoft.OpenApi;

public static class OpenApiService
{
    public static async Task<OpenApiDocument> GetOpenApiJsonAsync(int port)
    {
        var (openApiDoc, _) = await OpenApiDocument.LoadAsync($"http://localhost:{port}/openapi/v1.json");
        if (openApiDoc is null)
        {
            throw new Exception("Failed to load OpenAPI document.");
        }

        return openApiDoc;
    }
}