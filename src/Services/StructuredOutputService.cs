using System.Text.Encodings.Web;
using System.Text.Json;

namespace RouteCheck.Services;

/// <summary>
/// Writes the result of a run as one JSON or Markdown document (--output json/markdown).
/// The document is written as raw text, as Spectre would wrap long lines at the console width.
/// </summary>
public static class StructuredOutputService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        // Keeps quotes in paths and errors readable instead of writing them as Unicode escapes.
        // That is safe, as the document is not embedded in HTML.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <param name="project">The project file relative to the current directory, if it was found.</param>
    /// <param name="error">The error (plain text) that ended the run, if any.</param>
    public static void Write(TextWriter writer, string output, string? project, IReadOnlyList<Route> routes, string? error)
    {
        if (output == OutputFormat.Json)
        {
            var document = new CheckDocument("check", project, routes, error);
            writer.WriteLine(JsonSerializer.Serialize(document, JsonOptions));
            return;
        }

        var lines = new List<string>();

        if (project is not null)
        {
            lines.Add($"## `{project}`");
            lines.Add("");
        }

        if (error is null)
        {
            if (routes.Count == 0)
            {
                lines.Add("No routes found.");
            }
            else
            {
                lines.Add("| Method | Path |");
                lines.Add("|---|---|");
                lines.AddRange(routes.Select(r => $"| {r.Method} | `{r.Path.Replace("|", "\\|")}` |"));
            }
        }
        else
        {
            lines.Add($"**Error:** {error}");
        }

        foreach (var line in lines)
        {
            writer.WriteLine(line);
        }
    }

    private sealed record CheckDocument(string Command, string? Project, IReadOnlyList<Route> Routes, string? Error);
}
