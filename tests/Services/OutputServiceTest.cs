using Microsoft.OpenApi;

using Spectre.Console.Testing;

namespace RouteCheck.Tests.Services;

public class OutputServiceTest
{
    [Test]
    public Task Renders_Table_Correctly()
    {
        var openApiDocument = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Sample API",
                Version = "1.0.0"
            },
            Paths = new OpenApiPaths
            {
                ["/pets"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            Summary = "List all pets",
                        }
                    }
                }
            }
        };

        // Capture the console output
        TestConsole console = new();
        RouteCheck.Services.OutputService.DisplayRoutesFromOpenApi(console, openApiDocument);
        var output = console.Output.ToString();

        // Verify the output contains the expected table structure
        return Verify(output);
    }
}
