using Shouldly;
using Typewriter.CLI.Infrastructure;
using Xunit;

namespace Typewriter.CLI.Tests;

/// <summary>
/// Unit tests for <see cref="ProjectFileParser"/>.
/// Tests SDK-style detection and Compile include/exclude extraction.
/// </summary>
[Trait("Category", "CLI")]
[Trait("Component", "ProjectFileParser")]
public class ProjectFileParserTests
{
    private readonly ProjectFileParser _parser;
    private readonly string _testProjectsPath;

    public ProjectFileParserTests()
    {
        _parser = new ProjectFileParser(new SourceFileDiscovery());
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

    #region IsSdkStyleProject Tests (T013)

    [Fact]
    public void IsSdkStyleProject_WithSdkAttribute_ReturnsTrue()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.IsSdkStyleProject(projectPath);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsSdkStyleProject_WithLegacyFormat_ReturnsFalse()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "LegacyProject", "LegacyProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.IsSdkStyleProject(projectPath);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsSdkStyleProject_WithMixedSolutionSdkProject_ReturnsTrue()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "MixedSolution", "SdkProject", "SdkProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.IsSdkStyleProject(projectPath);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsSdkStyleProject_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "NonExistent.csproj");

        // Act
        var result = _parser.IsSdkStyleProject(projectPath);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Parse SDK-style Projects Tests (T014)

    [Fact]
    public void Parse_SdkStyleProject_ReturnsProjectInfo()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.Parse(projectPath);

        // Assert
        result.ShouldNotBeNull();
        result.IsSdkStyle.ShouldBeTrue();
        result.ProjectPath.ShouldBe(Path.GetFullPath(projectPath));
        result.ProjectName.ShouldBe("SdkStyleProject");
    }

    [Fact]
    public void Parse_SdkStyleProject_ExtractsTargetFramework()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.Parse(projectPath);

        // Assert
        result.ShouldNotBeNull();
        result.TargetFramework.ShouldBe("net8.0");
    }

    [Fact]
    public void Parse_SdkStyleProject_ExtractsRootNamespace()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.Parse(projectPath);

        // Assert
        result.ShouldNotBeNull();
        result.RootNamespace.ShouldBe("SdkStyleProject");
    }

    [Fact]
    public void Parse_SdkStyleProject_ExtractsAssemblyName()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.Parse(projectPath);

        // Assert
        result.ShouldNotBeNull();
        result.AssemblyName.ShouldBe("SdkStyleProject");
    }

    [Fact]
    public void Parse_SdkStyleProject_DiscoversCsFiles()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.Parse(projectPath);

        // Assert
        result.ShouldNotBeNull();
        result.SourceFiles.Count.ShouldBeGreaterThan(0);
        result.SourceFiles.ShouldContain(f => f.EndsWith("Class1.cs"));
        result.SourceFiles.ShouldContain(f => f.Contains("SubFolder") && f.EndsWith("Class2.cs"));
    }

    [Fact]
    public void Parse_SdkStyleProject_ExcludesObjAndBinFolders()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.Parse(projectPath);

        // Assert
        result.ShouldNotBeNull();
        result.SourceFiles.ShouldNotContain(f => f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar));
        result.SourceFiles.ShouldNotContain(f => f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));
    }

    #endregion

    #region Parse Legacy Projects Tests (T014)

    [Fact]
    public void Parse_LegacyProject_ReturnsProjectInfo()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "LegacyProject", "LegacyProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.Parse(projectPath);

        // Assert
        result.ShouldNotBeNull();
        result.IsSdkStyle.ShouldBeFalse();
        result.ProjectPath.ShouldBe(Path.GetFullPath(projectPath));
        result.ProjectName.ShouldBe("LegacyProject");
    }

    [Fact]
    public void Parse_LegacyProject_ExtractsExplicitCompileIncludes()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "LegacyProject", "LegacyProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.Parse(projectPath);

        // Assert
        result.ShouldNotBeNull();
        result.SourceFiles.Count.ShouldBe(2);
        result.SourceFiles.ShouldContain(f => f.EndsWith("Class1.cs"));
        result.SourceFiles.ShouldContain(f => f.EndsWith("Class2.cs"));
    }

    [Fact]
    public void Parse_LegacyProject_ExtractsRootNamespace()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "LegacyProject", "LegacyProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.Parse(projectPath);

        // Assert
        result.ShouldNotBeNull();
        result.RootNamespace.ShouldBe("LegacyProject");
    }

    [Fact]
    public void Parse_LegacyProject_ExtractsAssemblyName()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "LegacyProject", "LegacyProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.Parse(projectPath);

        // Assert
        result.ShouldNotBeNull();
        result.AssemblyName.ShouldBe("LegacyProject");
    }

    #endregion

    #region Parse Projects with External Files (T016a support)

    [Fact]
    public void Parse_SdkProjectWithExternalFile_IncludesExternalFile()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "MixedSolution", "SdkProject", "SdkProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.Parse(projectPath);

        // Assert
        result.ShouldNotBeNull();
        // Should include the external SharedClass.cs from ../SharedFiles/
        result.SourceFiles.ShouldContain(f => f.Contains("SharedClass.cs"));
    }

    [Fact]
    public void Parse_SdkProjectWithProjectReference_ExtractsProjectReferences()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "MixedSolution", "SdkProject", "SdkProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.Parse(projectPath);

        // Assert
        result.ShouldNotBeNull();
        result.ProjectReferences.Count.ShouldBe(1);
        result.ProjectReferences.ShouldContain(r => r.Contains("LegacyProject.csproj"));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Parse_NonExistentFile_ReturnsNull()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "NonExistent.csproj");

        // Act
        var result = _parser.Parse(projectPath);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Parse_ReturnsAbsoluteSourceFilePaths()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Act
        var result = _parser.Parse(projectPath);

        // Assert
        result.ShouldNotBeNull();
        result.SourceFiles.ShouldAllBe(f => Path.IsPathRooted(f));
    }

    #endregion
}
