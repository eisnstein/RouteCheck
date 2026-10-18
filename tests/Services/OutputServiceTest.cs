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

    [Test]
    public Task Renders_SeveralMethods_And_EscapesMarkupInPaths()
    {
        var openApiDocument = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/pets"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Get] = new OpenApiOperation(),
                        [HttpMethod.Post] = new OpenApiOperation(),
                    }
                },
                ["/pets/[legacy]"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Delete] = new OpenApiOperation(),
                    }
                }
            }
        };

        TestConsole console = new();
        RouteCheck.Services.OutputService.DisplayRoutesFromOpenApi(console, openApiDocument);

        return Verify(console.Output.ToString());
    }

    [Test]
    public Task Renders_EmptyTable_When_ThereAreNoPaths()
    {
        var openApiDocument = new OpenApiDocument { Paths = new OpenApiPaths() };

        TestConsole console = new();
        RouteCheck.Services.OutputService.DisplayRoutesFromOpenApi(console, openApiDocument);

        return Verify(console.Output.ToString());
    }
}
