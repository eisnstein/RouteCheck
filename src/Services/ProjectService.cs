using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RouteCheck.Services;

public static partial class ProjectService
{
    private static readonly string[] ProjectExtensions = [".csproj", ".fsproj", ".vbproj"];
    private static readonly string[] SolutionExtensions = [".sln", ".slnx"];

    private const string ProjectFiles = "*.csproj, *.fsproj, *.vbproj";

    /// <summary>
    /// Returns the full path of the project file, given either the project file itself,
    /// the directory containing it, or a solution (file or directory) with one web project.
    /// </summary>
    public static string FindProjectFile(string path)
    {
        if (File.Exists(path))
        {
            if (HasExtension(path, SolutionExtensions))
            {
                return FindWebProject(Path.GetFullPath(path));
            }

            if (!HasExtension(path, ProjectExtensions))
            {
                throw new RouteCheckException($"'{path}' is not a project file ({ProjectFiles}) or solution.");
            }

            return Path.GetFullPath(path);
        }

        if (!Directory.Exists(path))
        {
            throw new RouteCheckException($"The path '{path}' does not exist.");
        }

        string[] projectFiles = GetFiles(path, ProjectExtensions);

        return projectFiles switch
        {
            [var projectFile] => Path.GetFullPath(projectFile),
            [] => FindWebProjectInSolution(path),
            _ => throw new RouteCheckException(
                $"Found several project files in '{path}': {string.Join(", ", projectFiles.Select(Path.GetFileName))}. Use --path to choose one."),
        };
    }

    // E.g. the root directory of a repository, with the projects in src/
    private static string FindWebProjectInSolution(string directory)
    {
        return GetFiles(directory, SolutionExtensions) switch
        {
            [var solution] => FindWebProject(Path.GetFullPath(solution)),
            [] => throw new RouteCheckException($"No project file ({ProjectFiles}) or solution found in '{directory}'."),
            var solutions => throw new RouteCheckException(
                $"No project file, but several solutions found in '{directory}': {string.Join(", ", solutions.Select(Path.GetFileName))}. Use --path to choose one."),
        };
    }

    private static string FindWebProject(string solution)
    {
        var solutionDirectory = Path.GetDirectoryName(solution)!;
        var webProjects = GetProjectPaths(solution)
            .Select(project => Path.GetFullPath(Path.Combine(solutionDirectory, project.Replace('\\', Path.DirectorySeparatorChar))))
            .Where(project => HasExtension(project, ProjectExtensions) && File.Exists(project) && IsWebProject(project))
            .Distinct()
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return webProjects switch
        {
            [var project] => project,
            [] => throw new RouteCheckException($"The solution '{solution}' has no web project (Microsoft.NET.Sdk.Web). Use --path to choose the project."),
            _ => throw new RouteCheckException(
                $"The solution '{solution}' has several web projects: {string.Join(", ", webProjects.Select(p => Path.GetRelativePath(solutionDirectory, p)))}. Use --path to choose one."),
        };
    }

    private static IEnumerable<string> GetProjectPaths(string solution)
    {
        var content = File.ReadAllText(solution);

        if (HasExtension(solution, [".slnx"]))
        {
            try
            {
                return XDocument.Parse(content).Descendants("Project")
                    .Select(project => project.Attribute("Path")?.Value)
                    .OfType<string>()
                    .ToList();
            }
            catch (System.Xml.XmlException ex)
            {
                throw new RouteCheckException($"The solution '{solution}' could not be read: {ex.Message}");
            }
        }

        // Project("{type}") = "Name", "src\Api\Api.csproj", "{id}"
        return SlnProjectRegex().Matches(content).Select(match => match.Groups["path"].Value).ToList();
    }

    private static bool IsWebProject(string projectFile)
    {
        return WebSdkRegex().IsMatch(File.ReadAllText(projectFile));
    }

    private static string[] GetFiles(string directory, string[] extensions)
    {
        var files = Directory.GetFiles(directory).Where(file => HasExtension(file, extensions)).ToArray();
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        return files;
    }

    private static bool HasExtension(string path, string[] extensions)
    {
        return extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    [GeneratedRegex("""^Project\("[^"]*"\)\s*=\s*"[^"]*",\s*"(?<path>[^"]*)"\s*,""", RegexOptions.Multiline)]
    private static partial Regex SlnProjectRegex();

    // <Project Sdk="Microsoft.NET.Sdk.Web">, also with a version or as <Sdk Name="..." />,
    // but not Microsoft.NET.Sdk.WebAssembly
    [GeneratedRegex(@"Microsoft\.NET\.Sdk\.Web\b(?!\.)", RegexOptions.IgnoreCase)]
    private static partial Regex WebSdkRegex();
}
