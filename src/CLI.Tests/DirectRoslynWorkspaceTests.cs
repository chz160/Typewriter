using Microsoft.CodeAnalysis;
using Shouldly;
using Typewriter.CLI.Infrastructure;
using Xunit;

namespace Typewriter.CLI.Tests;

/// <summary>
/// Integration tests for <see cref="DirectRoslynWorkspace"/>.
/// Tests fast project and solution loading using direct Roslyn parsing.
/// </summary>
[Trait("Category", "CLI")]
[Trait("Component", "DirectRoslynWorkspace")]
public class DirectRoslynWorkspaceTests : IDisposable
{
    private readonly string _testProjectsPath;
    private DirectRoslynWorkspace? _workspace;

    public DirectRoslynWorkspaceTests()
    {
        _testProjectsPath = FindTestProjectsPath();
    }

    private static string FindTestProjectsPath()
    {
        var currentDir = AppContext.BaseDirectory;
        while (currentDir != null)
        {
            var testProjectsPath = Path.Combine(currentDir, "src", "Tests", "TestProjects");
            if (Directory.Exists(testProjectsPath))
            {
                return testProjectsPath;
            }
            currentDir = Path.GetDirectoryName(currentDir);
        }
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "src", "Tests", "TestProjects");
    }

    private static void SkipIfPathDoesNotExist(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            Assert.Fail($"Test path does not exist: {path}. Ensure test fixtures are created.");
        }
    }

    public void Dispose()
    {
        _workspace?.Dispose();
    }

    #region SDK-style Project Loading Tests (T015)

    [Fact]
    public async Task LoadProjectAsync_SdkStyleProject_ReturnsTrue()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new DirectRoslynWorkspace();

        // Act
        var result = await _workspace.LoadProjectAsync(projectPath);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task LoadProjectAsync_SdkStyleProject_CreatesProject()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new DirectRoslynWorkspace();

        // Act
        await _workspace.LoadProjectAsync(projectPath);

        // Assert
        _workspace.GetProjects().Count().ShouldBe(1);
        var project = _workspace.GetProjects().First();
        project.Name.ShouldBe("SdkStyleProject");
    }

    [Fact]
    public async Task LoadProjectAsync_SdkStyleProject_LoadsSourceFiles()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new DirectRoslynWorkspace();

        // Act
        await _workspace.LoadProjectAsync(projectPath);

        // Assert
        var documents = _workspace.GetAllDocuments().ToList();
        documents.Count.ShouldBeGreaterThan(0);
        documents.ShouldContain(d => d.Name == "Class1.cs");
        documents.ShouldContain(d => d.Name == "Class2.cs");
    }

    [Fact]
    public async Task LoadProjectAsync_SdkStyleProject_CanGetCompilation()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new DirectRoslynWorkspace();
        await _workspace.LoadProjectAsync(projectPath);

        // Act
        var project = _workspace.GetProjects().First();
        var compilation = await _workspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        compilation.SyntaxTrees.Count().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task LoadProjectAsync_SdkStyleProject_CompilationContainsTypes()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new DirectRoslynWorkspace();
        await _workspace.LoadProjectAsync(projectPath);

        // Act
        var project = _workspace.GetProjects().First();
        var compilation = await _workspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        var class1Symbol = compilation.GetTypeByMetadataName("SdkStyleProject.Class1");
        class1Symbol.ShouldNotBeNull();
    }

    #endregion

    #region Legacy Project Loading Tests (T016)

    [Fact]
    public async Task LoadProjectAsync_LegacyProject_ReturnsTrue()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "LegacyProject", "LegacyProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new DirectRoslynWorkspace();

        // Act
        var result = await _workspace.LoadProjectAsync(projectPath);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task LoadProjectAsync_LegacyProject_CreatesProject()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "LegacyProject", "LegacyProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new DirectRoslynWorkspace();

        // Act
        await _workspace.LoadProjectAsync(projectPath);

        // Assert
        _workspace.GetProjects().Count().ShouldBe(1);
        var project = _workspace.GetProjects().First();
        project.Name.ShouldBe("LegacyProject");
    }

    [Fact]
    public async Task LoadProjectAsync_LegacyProject_LoadsExplicitCompileIncludes()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "LegacyProject", "LegacyProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new DirectRoslynWorkspace();

        // Act
        await _workspace.LoadProjectAsync(projectPath);

        // Assert
        var documents = _workspace.GetAllDocuments().ToList();
        documents.Count.ShouldBe(2);
        documents.ShouldContain(d => d.Name == "Class1.cs");
        documents.ShouldContain(d => d.Name == "Class2.cs");
    }

    [Fact]
    public async Task LoadProjectAsync_LegacyProject_CanGetCompilation()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "LegacyProject", "LegacyProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new DirectRoslynWorkspace();
        await _workspace.LoadProjectAsync(projectPath);

        // Act
        var project = _workspace.GetProjects().First();
        var compilation = await _workspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        compilation.SyntaxTrees.Count().ShouldBe(2);
    }

    #endregion

    #region External File Reference Tests (T016a)

    [Fact]
    public async Task LoadProjectAsync_ProjectWithExternalFile_IncludesExternalFile()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "MixedSolution", "SdkProject", "SdkProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new DirectRoslynWorkspace();

        // Act
        await _workspace.LoadProjectAsync(projectPath);

        // Assert
        var documents = _workspace.GetAllDocuments().ToList();
        // Should include ProjectClass.cs from project directory AND SharedClass.cs from ../SharedFiles/
        documents.ShouldContain(d => d.Name == "ProjectClass.cs");
        documents.ShouldContain(d => d.Name == "SharedClass.cs");
    }

    [Fact]
    public async Task LoadProjectAsync_ProjectWithExternalFile_ExternalFileIsResolvable()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "MixedSolution", "SdkProject", "SdkProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new DirectRoslynWorkspace();
        await _workspace.LoadProjectAsync(projectPath);

        // Act
        var project = _workspace.GetProjects().First();
        var compilation = await _workspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        // Should be able to resolve SharedClass type from external file
        var sharedClassSymbol = compilation.GetTypeByMetadataName("MixedSolution.Shared.SharedClass");
        sharedClassSymbol.ShouldNotBeNull();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task LoadProjectAsync_NonExistentProject_ReturnsFalse()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "NonExistent.csproj");
        _workspace = new DirectRoslynWorkspace();

        // Act
        var result = await _workspace.LoadProjectAsync(projectPath);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task LoadProjectAsync_NonExistentProject_AddsDiagnostic()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "NonExistent.csproj");
        _workspace = new DirectRoslynWorkspace();

        // Act
        await _workspace.LoadProjectAsync(projectPath);

        // Assert
        _workspace.Diagnostics.HasErrors.ShouldBeTrue();
    }

    #endregion

    #region Workspace State Tests

    [Fact]
    public async Task LoadProjectAsync_SetsProjectPath()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new DirectRoslynWorkspace();

        // Act
        await _workspace.LoadProjectAsync(projectPath);

        // Assert
        _workspace.ProjectPath.ShouldBe(Path.GetFullPath(projectPath));
    }

    [Fact]
    public void Workspace_IsAccessible()
    {
        // Arrange
        _workspace = new DirectRoslynWorkspace();

        // Assert
        _workspace.Workspace.ShouldNotBeNull();
    }

    [Fact]
    public void Solution_IsAccessible()
    {
        // Arrange
        _workspace = new DirectRoslynWorkspace();

        // Assert
        _workspace.Solution.ShouldNotBeNull();
    }

    #endregion

    #region Solution Loading Tests (T025)

    [Fact]
    public async Task LoadSolutionAsync_ValidSolution_ReturnsTrue()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);
        _workspace = new DirectRoslynWorkspace();

        // Act
        var result = await _workspace.LoadSolutionAsync(solutionPath);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task LoadSolutionAsync_ValidSolution_LoadsAllProjects()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);
        _workspace = new DirectRoslynWorkspace();

        // Act
        await _workspace.LoadSolutionAsync(solutionPath);

        // Assert
        var projects = _workspace.GetProjects().ToList();
        projects.Count.ShouldBe(2); // SdkProject and LegacyProject
        projects.ShouldContain(p => p.Name == "SdkProject");
        projects.ShouldContain(p => p.Name == "LegacyProject");
    }

    [Fact]
    public async Task LoadSolutionAsync_ValidSolution_LoadsAllDocuments()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);
        _workspace = new DirectRoslynWorkspace();

        // Act
        await _workspace.LoadSolutionAsync(solutionPath);

        // Assert
        var documents = _workspace.GetAllDocuments().ToList();
        // SdkProject has ProjectClass.cs and SharedClass.cs, LegacyProject has LegacyClass.cs
        documents.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task LoadSolutionAsync_ValidSolution_SetsSolutionPath()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);
        _workspace = new DirectRoslynWorkspace();

        // Act
        await _workspace.LoadSolutionAsync(solutionPath);

        // Assert
        _workspace.SolutionPath.ShouldBe(Path.GetFullPath(solutionPath));
    }

    [Fact]
    public async Task LoadSolutionAsync_WithProjectReferences_CanResolveTypes()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);
        _workspace = new DirectRoslynWorkspace();
        await _workspace.LoadSolutionAsync(solutionPath);

        // Act
        var sdkProject = _workspace.GetProjects().First(p => p.Name == "SdkProject");
        var compilation = await _workspace.GetCompilationAsync(sdkProject);

        // Assert
        compilation.ShouldNotBeNull();
        // SdkProject references LegacyProject, so should be able to resolve LegacyClass
        var legacyClassSymbol = compilation.GetTypeByMetadataName("MixedSolution.LegacyProject.LegacyClass");
        legacyClassSymbol.ShouldNotBeNull();
    }

    [Fact]
    public async Task LoadSolutionAsync_WithProjectReferences_ProjectsHaveReferences()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);
        _workspace = new DirectRoslynWorkspace();

        // Act
        await _workspace.LoadSolutionAsync(solutionPath);

        // Assert
        var sdkProject = _workspace.GetProjects().First(p => p.Name == "SdkProject");
        // Should have a project reference to LegacyProject
        sdkProject.ProjectReferences.Count().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task LoadSolutionAsync_NonExistentSolution_ReturnsFalse()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "NonExistent.sln");
        _workspace = new DirectRoslynWorkspace();

        // Act
        var result = await _workspace.LoadSolutionAsync(solutionPath);

        // Assert
        result.ShouldBeFalse();
        _workspace.Diagnostics.HasErrors.ShouldBeTrue();
    }

    #endregion
}
