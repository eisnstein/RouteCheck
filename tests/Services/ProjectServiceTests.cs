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
    public async Task FindProjectFile_Throws_When_PathDoesNotExist()
    {
        using var dir = TestHelper.CreateTempDirectory();

        await Assert.That(() => ProjectService.FindProjectFile(Path.Combine(dir.Path, "missing")))
            .Throws<RouteCheckException>()
            .WithMessageContaining("does not exist");
    }
}
