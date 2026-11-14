using System.Text.Encodings.Web;
using System.Text.Json;

namespace RouteCheck.Services;

/// <summary>
/// The result of a check, as written to the documents.
/// </summary>
/// <param name="Project">The project file relative to the current directory, if it was found.</param>
/// <param name="Problems">What is wrong with the routes, e.g. ambiguous routes.</param>
/// <param name="Error">The error (plain text) that ended the run, if any.</param>
public sealed record CheckResult(
    string? Project,
    IReadOnlyList<Route> Routes,
    IReadOnlyList<RouteProblem> Problems,
    string? Error)
{
    public static CheckResult Failed(string? project, string error) => new(project, [], [], error);
}

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

    public static void Write(TextWriter writer, string output, CheckResult result)
    {
        if (output == OutputFormat.Json)
        {
            var document = new CheckDocument("check", result.Project, result.Routes, result.Problems, result.Error);
            writer.WriteLine(JsonSerializer.Serialize(document, JsonOptions));
            return;
        }

        var lines = new List<string>();

        if (result.Project is not null)
        {
            lines.Add($"## `{result.Project}`");
            lines.Add("");
        }

        if (result.Error is null)
        {
            if (result.Routes.Count == 0)
            {
                lines.Add("No routes found.");
            }
            else
            {
                if (OutputService.ShowAuth(result.Routes))
                {
                    lines.Add("| Method | Path | Handler | Auth |");
                    lines.Add("|---|---|---|---|");
                    lines.AddRange(result.Routes.Select(r => $"| {r.Method} | `{Escape(r.Path)}` | {Escape(r.Handler ?? "")} | {Escape(r.Auth ?? "")} |"));
                }
                else
                {
                    lines.Add("| Method | Path | Handler |");
                    lines.Add("|---|---|---|");
                    lines.AddRange(result.Routes.Select(r => $"| {r.Method} | `{Escape(r.Path)}` | {Escape(r.Handler ?? "")} |"));
                }
            }

            if (result.Problems.Count > 0)
            {
                lines.Add("");
                lines.Add("### Problems");
                lines.Add("");
                lines.AddRange(result.Problems.Select(p => $"- {p.Message}"));
            }
        }
        else
        {
            lines.Add($"**Error:** {result.Error}");
        }

        foreach (var line in lines)
        {
            writer.WriteLine(line);
        }
    }

    private static string Escape(string cell) => cell.Replace("|", "\\|");

    private sealed record CheckDocument(
        string Command,
        string? Project,
        IReadOnlyList<Route> Routes,
        IReadOnlyList<RouteProblem> Problems,
        string? Error);
}
