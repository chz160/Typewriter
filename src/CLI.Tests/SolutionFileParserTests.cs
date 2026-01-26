using Shouldly;
using Typewriter.CLI.Infrastructure;
using Typewriter.CLI.Infrastructure.Models;
using Xunit;

namespace Typewriter.CLI.Tests;

/// <summary>
/// Unit tests for <see cref="SolutionFileParser"/>.
/// Tests solution file parsing and project extraction.
/// </summary>
[Trait("Category", "CLI")]
[Trait("Component", "SolutionFileParser")]
public class SolutionFileParserTests
{
    private readonly SolutionFileParser _parser;
    private readonly string _testProjectsPath;

    public SolutionFileParserTests()
    {
        _parser = new SolutionFileParser();
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

    #region Parse Tests (T023)

    [Fact]
    public void Parse_ValidSolution_ReturnsSolutionInfo()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);

        // Act
        var result = _parser.Parse(solutionPath);

        // Assert
        result.ShouldNotBeNull();
        result.SolutionPath.ShouldBe(Path.GetFullPath(solutionPath));
        result.SolutionName.ShouldBe("MixedSolution");
    }

    [Fact]
    public void Parse_ValidSolution_ExtractsSolutionDirectory()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);

        // Act
        var result = _parser.Parse(solutionPath);

        // Assert
        result.ShouldNotBeNull();
        result.SolutionDirectory.ShouldBe(Path.GetDirectoryName(Path.GetFullPath(solutionPath)));
    }

    [Fact]
    public void Parse_ValidSolution_ExtractsAllProjects()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);

        // Act
        var result = _parser.Parse(solutionPath);

        // Assert
        result.ShouldNotBeNull();
        // Should have 3 entries: SdkProject, LegacyProject, Solution Items folder
        result.Projects.Count.ShouldBe(3);
    }

    [Fact]
    public void Parse_ValidSolution_ExtractsProjectNames()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);

        // Act
        var result = _parser.Parse(solutionPath);

        // Assert
        result.ShouldNotBeNull();
        result.Projects.ShouldContain(p => p.ProjectName == "SdkProject");
        result.Projects.ShouldContain(p => p.ProjectName == "LegacyProject");
    }

    [Fact]
    public void Parse_ValidSolution_ExtractsProjectPaths()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);

        // Act
        var result = _parser.Parse(solutionPath);

        // Assert
        result.ShouldNotBeNull();
        result.Projects.ShouldContain(p => p.RelativePath.Contains("SdkProject.csproj"));
        result.Projects.ShouldContain(p => p.RelativePath.Contains("LegacyProject.csproj"));
    }

    [Fact]
    public void Parse_ValidSolution_ExtractsProjectGuids()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);

        // Act
        var result = _parser.Parse(solutionPath);

        // Assert
        result.ShouldNotBeNull();
        var csharpProjects = result.Projects.Where(p => p.IsCSharpProject).ToList();
        csharpProjects.ShouldAllBe(p => p.ProjectGuid != Guid.Empty);
    }

    [Fact]
    public void Parse_ValidSolution_ExtractsProjectTypeGuids()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);

        // Act
        var result = _parser.Parse(solutionPath);

        // Assert
        result.ShouldNotBeNull();
        var sdkProject = result.Projects.First(p => p.ProjectName == "SdkProject");
        sdkProject.ProjectTypeGuid.ShouldBe(SolutionProject.CSharpProjectTypeGuid);
    }

    [Fact]
    public void Parse_NonExistentFile_ReturnsNull()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "NonExistent.sln");

        // Act
        var result = _parser.Parse(solutionPath);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region Solution Folder Filtering Tests (T024)

    [Fact]
    public void Parse_SolutionWithFolders_IdentifiesSolutionFolders()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);

        // Act
        var result = _parser.Parse(solutionPath);

        // Assert
        result.ShouldNotBeNull();
        var solutionFolders = result.Projects.Where(p => p.IsSolutionFolder).ToList();
        solutionFolders.Count.ShouldBeGreaterThan(0);
        solutionFolders.ShouldContain(p => p.ProjectName == "Solution Items");
    }

    [Fact]
    public void GetCSharpProjects_FiltersSolutionFolders()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);

        // Act
        var result = _parser.Parse(solutionPath);

        // Assert
        result.ShouldNotBeNull();
        var csharpProjects = result.GetCSharpProjects().ToList();
        csharpProjects.Count.ShouldBe(2); // Only SdkProject and LegacyProject
        csharpProjects.ShouldNotContain(p => p.IsSolutionFolder);
    }

    [Fact]
    public void GetCSharpProjectPaths_ReturnsAbsolutePaths()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);

        // Act
        var result = _parser.Parse(solutionPath);

        // Assert
        result.ShouldNotBeNull();
        var paths = result.GetCSharpProjectPaths().ToList();
        paths.Count.ShouldBe(2);
        paths.ShouldAllBe(p => Path.IsPathRooted(p));
    }

    [Fact]
    public void GetCSharpProjectPaths_ReturnsExistingFiles()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);

        // Act
        var result = _parser.Parse(solutionPath);

        // Assert
        result.ShouldNotBeNull();
        var paths = result.GetCSharpProjectPaths().ToList();
        paths.ShouldAllBe(p => File.Exists(p));
    }

    #endregion

    #region GetCSharpProjectPaths Interface Method Tests

    [Fact]
    public void GetCSharpProjectPaths_Interface_ReturnsProjectPaths()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);

        // Act
        var paths = _parser.GetCSharpProjectPaths(solutionPath).ToList();

        // Assert
        paths.Count.ShouldBe(2);
        paths.ShouldContain(p => p.EndsWith("SdkProject.csproj"));
        paths.ShouldContain(p => p.EndsWith("LegacyProject.csproj"));
    }

    [Fact]
    public void GetCSharpProjectPaths_Interface_NonExistentFile_ReturnsEmpty()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "NonExistent.sln");

        // Act
        var paths = _parser.GetCSharpProjectPaths(solutionPath).ToList();

        // Assert
        paths.ShouldBeEmpty();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Parse_EmptyPath_ReturnsNull()
    {
        // Act
        var result = _parser.Parse(string.Empty);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Parse_NullPath_ReturnsNull()
    {
        // Act
        var result = _parser.Parse(null!);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetCSharpProjectPaths_EmptyPath_ReturnsEmpty()
    {
        // Act
        var paths = _parser.GetCSharpProjectPaths(string.Empty).ToList();

        // Assert
        paths.ShouldBeEmpty();
    }

    #endregion
}
