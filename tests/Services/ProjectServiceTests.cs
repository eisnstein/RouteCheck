using RouteCheck.Services;

namespace RouteCheck.Tests.Services;

public class ProjectServiceTests
{
    [Test]
    public async Task FindProjectFile_Returns_TheProjectFile_InADirectory()
    {
        using var dir = TestHelper.CreateTempDirectory();
        var projectFile = Path.Combine(dir.Path, "Api.csproj");
        File.WriteAllText(projectFile, "<Project />");

        await Assert.That(ProjectService.FindProjectFile(dir.Path)).IsEqualTo(projectFile);
    }

    [Test]
    public async Task FindProjectFile_Returns_AGivenProjectFile()
    {
        using var dir = TestHelper.CreateTempDirectory();
        File.WriteAllText(Path.Combine(dir.Path, "Api.csproj"), "<Project />");
        var projectFile = Path.Combine(dir.Path, "Other.csproj");
        File.WriteAllText(projectFile, "<Project />");

        await Assert.That(ProjectService.FindProjectFile(projectFile)).IsEqualTo(projectFile);
    }

    [Test]
    public async Task FindProjectFile_Throws_When_DirectoryHasNoProjectFile()
    {
        using var dir = TestHelper.CreateTempDirectory();
        File.WriteAllText(Path.Combine(dir.Path, "Program.cs"), "");

        await Assert.That(() => ProjectService.FindProjectFile(dir.Path))
            .Throws<RouteCheckException>()
            .WithMessageContaining("No project file");
    }

    [Test]
    public async Task FindProjectFile_Throws_And_ListsThem_When_DirectoryHasSeveralProjectFiles()
    {
        using var dir = TestHelper.CreateTempDirectory();
        File.WriteAllText(Path.Combine(dir.Path, "B.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(dir.Path, "A.csproj"), "<Project />");

        await Assert.That(() => ProjectService.FindProjectFile(dir.Path))
            .Throws<RouteCheckException>()
            .WithMessageContaining("A.csproj, B.csproj");
    }

    [Test]
    public async Task FindProjectFile_Throws_When_FileIsNotAProjectFile()
    {
        using var dir = TestHelper.CreateTempDirectory();
        var file = Path.Combine(dir.Path, "Program.cs");
        File.WriteAllText(file, "");

        await Assert.That(() => ProjectService.FindProjectFile(file))
            .Throws<RouteCheckException>()
            .WithMessageContaining("is not a project file");
    }

    [Test]
    [Arguments("Api.fsproj")]
    [Arguments("Api.vbproj")]
    public async Task FindProjectFile_Returns_FSharpAndVisualBasicProjects(string name)
    {
        using var dir = TestHelper.CreateTempDirectory();
        var projectFile = Path.Combine(dir.Path, name);
        File.WriteAllText(projectFile, "<Project />");

        await Assert.That(ProjectService.FindProjectFile(dir.Path)).IsEqualTo(projectFile);
    }

    private const string WebProject = """<Project Sdk="Microsoft.NET.Sdk.Web" />""";
    private const string LibraryProject = """<Project Sdk="Microsoft.NET.Sdk" />""";

    [Test]
    public async Task FindProjectFile_Returns_TheWebProject_OfTheSolution_InADirectory()
    {
        using var dir = TestHelper.CreateTempDirectory();
        var api = CreateProject(dir, "src/Api/Api.csproj", WebProject);
        CreateProject(dir, "src/Core/Core.csproj", LibraryProject);
        CreateProject(dir, "src/Client/Client.csproj", """<Project Sdk="Microsoft.NET.Sdk.WebAssembly" />""");
        File.WriteAllText(Path.Combine(dir.Path, "App.sln"), """

            Microsoft Visual Studio Solution File, Format Version 12.00
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Api", "src\Api\Api.csproj", "{6E0F0F5B-1C1A-4C55-9C34-1B1B9B1D1E01}"
            EndProject
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Core", "src\Core\Core.csproj", "{6E0F0F5B-1C1A-4C55-9C34-1B1B9B1D1E02}"
            EndProject
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Client", "src\Client\Client.csproj", "{6E0F0F5B-1C1A-4C55-9C34-1B1B9B1D1E03}"
            EndProject
            """);

        await Assert.That(ProjectService.FindProjectFile(dir.Path)).IsEqualTo(api);
    }

    [Test]
    public async Task FindProjectFile_Returns_TheWebProject_OfAGivenSlnxSolution()
    {
        using var dir = TestHelper.CreateTempDirectory();
        var api = CreateProject(dir, "src/Api/Api.csproj", """<Project Sdk="Microsoft.NET.Sdk.Web/10.0.100" />""");
        CreateProject(dir, "tests/Api.Tests/Api.Tests.csproj", LibraryProject);
        var solution = Path.Combine(dir.Path, "App.slnx");
        File.WriteAllText(solution, """
            <Solution>
              <Folder Name="/src/">
                <Project Path="src/Api/Api.csproj" />
              </Folder>
              <Project Path="tests/Api.Tests/Api.Tests.csproj" />
            </Solution>
            """);

        await Assert.That(ProjectService.FindProjectFile(solution)).IsEqualTo(api);
    }

    [Test]
    public async Task FindProjectFile_Throws_And_ListsThem_When_TheSolutionHasSeveralWebProjects()
    {
        using var dir = TestHelper.CreateTempDirectory();
        CreateProject(dir, "Api/Api.csproj", WebProject);
        CreateProject(dir, "Admin/Admin.csproj", WebProject);
        File.WriteAllText(Path.Combine(dir.Path, "App.slnx"), """
            <Solution>
              <Project Path="Api/Api.csproj" />
              <Project Path="Admin/Admin.csproj" />
            </Solution>
            """);

        await Assert.That(() => ProjectService.FindProjectFile(dir.Path))
            .Throws<RouteCheckException>()
            .WithMessageContaining($"several web projects: {Path.Combine("Admin", "Admin.csproj")}, {Path.Combine("Api", "Api.csproj")}");
    }

    [Test]
    public async Task FindProjectFile_Throws_When_TheSolutionHasNoWebProject()
    {
        using var dir = TestHelper.CreateTempDirectory();
        CreateProject(dir, "Core/Core.csproj", LibraryProject);
        File.WriteAllText(Path.Combine(dir.Path, "App.slnx"), """<Solution><Project Path="Core/Core.csproj" /></Solution>""");

        await Assert.That(() => ProjectService.FindProjectFile(dir.Path))
            .Throws<RouteCheckException>()
            .WithMessageContaining("has no web project");
    }

    private static string CreateProject(TestHelper.TempDirectory dir, string path, string content)
    {
        var projectFile = Path.GetFullPath(Path.Combine(dir.Path, path));
        Directory.CreateDirectory(Path.GetDirectoryName(projectFile)!);
        File.WriteAllText(projectFile, content);
        return projectFile;
    }

    [Test]
    public async Task FindProjectFile_Throws_When_PathDoesNotExist()
    {
        using var dir = TestHelper.CreateTempDirectory();

        await Assert.That(() => ProjectService.FindProjectFile(Path.Combine(dir.Path, "missing")))
            .Throws<RouteCheckException>()
            .WithMessageContaining("does not exist");
    }
}
