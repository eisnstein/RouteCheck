namespace RouteCheck.Services;

public static class ProjectService
{
    /// <summary>
    /// Returns the full path of the project file, given either the project file itself
    /// or the directory containing it.
    /// </summary>
    public static string FindProjectFile(string path)
    {
        if (File.Exists(path))
        {
            if (!Path.GetExtension(path).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                throw new RouteCheckException($"'{path}' is not a project file (*.csproj).");
            }

            return Path.GetFullPath(path);
        }

        if (!Directory.Exists(path))
        {
            throw new RouteCheckException($"The path '{path}' does not exist.");
        }

        string[] projectFiles = Directory.GetFiles(path, "*.csproj");
        Array.Sort(projectFiles, StringComparer.OrdinalIgnoreCase);

        return projectFiles switch
        {
            [var projectFile] => Path.GetFullPath(projectFile),
            [] => throw new RouteCheckException($"No project file (*.csproj) found in '{path}'."),
            _ => throw new RouteCheckException(
                $"Found several project files in '{path}': {string.Join(", ", projectFiles.Select(Path.GetFileName))}. Use --path to choose one."),
        };
    }
}
