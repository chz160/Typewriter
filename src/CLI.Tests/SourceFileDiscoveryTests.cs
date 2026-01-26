using Shouldly;
using Typewriter.CLI.Infrastructure;
using Xunit;

namespace Typewriter.CLI.Tests;

/// <summary>
/// Unit tests for <see cref="SourceFileDiscovery"/>.
/// Tests include patterns, exclude patterns, and default obj/bin exclusion.
/// </summary>
[Trait("Category", "CLI")]
[Trait("Component", "SourceFileDiscovery")]
public class SourceFileDiscoveryTests
{
    private readonly SourceFileDiscovery _discovery;
    private readonly string _testProjectsPath;

    public SourceFileDiscoveryTests()
    {
        _discovery = new SourceFileDiscovery();
        // Get the path to test projects relative to source
        _testProjectsPath = FindTestProjectsPath();
    }

    private static string FindTestProjectsPath()
    {
        // Walk up from the current directory to find the test projects
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

        // Fallback: try relative to source root
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "src", "Tests", "TestProjects");
    }

    #region SDK-style File Discovery

    [Fact]
    public void DiscoverSdkStyleFiles_WithDefaultPatterns_FindsAllCsFiles()
    {
        // Arrange
        var sdkProjectPath = Path.Combine(_testProjectsPath, "SdkStyleProject");
        SkipIfPathDoesNotExist(sdkProjectPath);

        // Act
        var result = _discovery.DiscoverSdkStyleFiles(sdkProjectPath);

        // Assert
        result.IncludedFiles.Count.ShouldBeGreaterThan(0);
        result.IncludedFiles.ShouldAllBe(f => f.EndsWith(".cs"));
    }

    [Fact]
    public void DiscoverSdkStyleFiles_WithDefaultPatterns_IncludesFilesInSubfolders()
    {
        // Arrange
        var sdkProjectPath = Path.Combine(_testProjectsPath, "SdkStyleProject");
        SkipIfPathDoesNotExist(sdkProjectPath);

        // Act
        var result = _discovery.DiscoverSdkStyleFiles(sdkProjectPath);

        // Assert - should find Class1.cs in root and Class2.cs in SubFolder
        result.IncludedFiles.ShouldContain(f => f.Contains("Class1.cs"));
        result.IncludedFiles.ShouldContain(f => f.Contains("SubFolder") && f.Contains("Class2.cs"));
    }

    [Fact]
    public void DiscoverSdkStyleFiles_ExcludesObjFolder_ByDefault()
    {
        // Arrange
        var sdkProjectPath = Path.Combine(_testProjectsPath, "SdkStyleProject");
        SkipIfPathDoesNotExist(sdkProjectPath);

        // Act
        var result = _discovery.DiscoverSdkStyleFiles(sdkProjectPath);

        // Assert
        result.IncludedFiles.ShouldNotContain(f => f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar));
    }

    [Fact]
    public void DiscoverSdkStyleFiles_ExcludesBinFolder_ByDefault()
    {
        // Arrange
        var sdkProjectPath = Path.Combine(_testProjectsPath, "SdkStyleProject");
        SkipIfPathDoesNotExist(sdkProjectPath);

        // Act
        var result = _discovery.DiscoverSdkStyleFiles(sdkProjectPath);

        // Assert
        result.IncludedFiles.ShouldNotContain(f => f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));
    }

    [Fact]
    public void DiscoverSdkStyleFiles_WithCustomExcludePattern_ExcludesMatching()
    {
        // Arrange
        var sdkProjectPath = Path.Combine(_testProjectsPath, "SdkStyleProject");
        SkipIfPathDoesNotExist(sdkProjectPath);
        var customExcludes = new[] { "**/SubFolder/**" };

        // Act
        var result = _discovery.DiscoverSdkStyleFiles(
            sdkProjectPath,
            excludePatterns: customExcludes);

        // Assert
        result.IncludedFiles.ShouldNotContain(f => f.Contains("SubFolder"));
        result.IncludedFiles.ShouldContain(f => f.Contains("Class1.cs"));
    }

    [Fact]
    public void DiscoverSdkStyleFiles_WithRemovePattern_RemovesMatchingFiles()
    {
        // Arrange
        var sdkProjectPath = Path.Combine(_testProjectsPath, "SdkStyleProject");
        SkipIfPathDoesNotExist(sdkProjectPath);
        var removePatterns = new[] { "**/Class1.cs" };

        // Act
        var result = _discovery.DiscoverSdkStyleFiles(
            sdkProjectPath,
            removePatterns: removePatterns);

        // Assert
        result.IncludedFiles.ShouldNotContain(f => f.Contains("Class1.cs"));
        result.RemovedFiles.ShouldContain(f => f.Contains("Class1.cs"));
    }

    [Fact]
    public void DiscoverSdkStyleFiles_ReturnsAbsolutePaths()
    {
        // Arrange
        var sdkProjectPath = Path.Combine(_testProjectsPath, "SdkStyleProject");
        SkipIfPathDoesNotExist(sdkProjectPath);

        // Act
        var result = _discovery.DiscoverSdkStyleFiles(sdkProjectPath);

        // Assert
        result.IncludedFiles.ShouldAllBe(f => Path.IsPathRooted(f));
    }

    [Fact]
    public void DiscoverSdkStyleFiles_ReportsPatternsUsed()
    {
        // Arrange
        var sdkProjectPath = Path.Combine(_testProjectsPath, "SdkStyleProject");
        SkipIfPathDoesNotExist(sdkProjectPath);

        // Act
        var result = _discovery.DiscoverSdkStyleFiles(sdkProjectPath);

        // Assert
        result.PatternsUsed.ShouldNotBeEmpty();
        result.PatternsUsed.ShouldContain(p => p.StartsWith("+")); // Include patterns
        result.PatternsUsed.ShouldContain(p => p.StartsWith("-")); // Exclude patterns
    }

    [Fact]
    public void DiscoverSdkStyleFiles_TracksDiscoveryTime()
    {
        // Arrange
        var sdkProjectPath = Path.Combine(_testProjectsPath, "SdkStyleProject");
        SkipIfPathDoesNotExist(sdkProjectPath);

        // Act
        var result = _discovery.DiscoverSdkStyleFiles(sdkProjectPath);

        // Assert
        result.DiscoveryTime.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    #endregion

    #region Legacy File Discovery

    [Fact]
    public void DiscoverLegacyFiles_WithExplicitIncludes_FindsSpecifiedFiles()
    {
        // Arrange
        var legacyProjectPath = Path.Combine(_testProjectsPath, "LegacyProject");
        SkipIfPathDoesNotExist(legacyProjectPath);
        var explicitIncludes = new[] { "Class1.cs", "Class2.cs" };

        // Act
        var result = _discovery.DiscoverLegacyFiles(legacyProjectPath, explicitIncludes);

        // Assert
        result.IncludedFiles.Count.ShouldBe(2);
        result.IncludedFiles.ShouldContain(f => f.Contains("Class1.cs"));
        result.IncludedFiles.ShouldContain(f => f.Contains("Class2.cs"));
    }

    [Fact]
    public void DiscoverLegacyFiles_WithGlobPattern_ExpandsPattern()
    {
        // Arrange
        var legacyProjectPath = Path.Combine(_testProjectsPath, "LegacyProject");
        SkipIfPathDoesNotExist(legacyProjectPath);
        var explicitIncludes = new[] { "*.cs" };

        // Act
        var result = _discovery.DiscoverLegacyFiles(legacyProjectPath, explicitIncludes);

        // Assert
        result.IncludedFiles.Count.ShouldBeGreaterThan(0);
        result.IncludedFiles.ShouldAllBe(f => f.EndsWith(".cs"));
    }

    [Fact]
    public void DiscoverLegacyFiles_WithMissingFile_TracksAsExcluded()
    {
        // Arrange
        var legacyProjectPath = Path.Combine(_testProjectsPath, "LegacyProject");
        SkipIfPathDoesNotExist(legacyProjectPath);
        var explicitIncludes = new[] { "NonExistent.cs" };

        // Act
        var result = _discovery.DiscoverLegacyFiles(legacyProjectPath, explicitIncludes);

        // Assert
        result.IncludedFiles.ShouldBeEmpty();
        result.RemovedFiles.ShouldContain(f => f.Contains("NonExistent.cs"));
    }

    [Fact]
    public void DiscoverLegacyFiles_RemovesDuplicates()
    {
        // Arrange
        var legacyProjectPath = Path.Combine(_testProjectsPath, "LegacyProject");
        SkipIfPathDoesNotExist(legacyProjectPath);
        var explicitIncludes = new[] { "Class1.cs", "Class1.cs", "*.cs" };

        // Act
        var result = _discovery.DiscoverLegacyFiles(legacyProjectPath, explicitIncludes);

        // Assert
        var class1Files = result.IncludedFiles.Count(f => f.Contains("Class1.cs"));
        class1Files.ShouldBe(1);
    }

    [Fact]
    public void DiscoverLegacyFiles_ReturnsAbsolutePaths()
    {
        // Arrange
        var legacyProjectPath = Path.Combine(_testProjectsPath, "LegacyProject");
        SkipIfPathDoesNotExist(legacyProjectPath);
        var explicitIncludes = new[] { "Class1.cs" };

        // Act
        var result = _discovery.DiscoverLegacyFiles(legacyProjectPath, explicitIncludes);

        // Assert
        result.IncludedFiles.ShouldAllBe(f => Path.IsPathRooted(f));
    }

    [Fact]
    public void DiscoverLegacyFiles_ReportsPatternsUsed()
    {
        // Arrange
        var legacyProjectPath = Path.Combine(_testProjectsPath, "LegacyProject");
        SkipIfPathDoesNotExist(legacyProjectPath);
        var explicitIncludes = new[] { "Class1.cs", "Class2.cs" };

        // Act
        var result = _discovery.DiscoverLegacyFiles(legacyProjectPath, explicitIncludes);

        // Assert
        result.PatternsUsed.Count.ShouldBe(2);
        result.PatternsUsed.ShouldAllBe(p => p.StartsWith("+"));
    }

    #endregion

    #region Default Pattern Constants

    [Fact]
    public void DefaultSdkIncludes_ContainsCsPattern()
    {
        SourceFileDiscovery.DefaultSdkIncludes.ShouldContain("**/*.cs");
    }

    [Fact]
    public void DefaultSdkExcludes_ContainsObjPattern()
    {
        SourceFileDiscovery.DefaultSdkExcludes.ShouldContain("**/obj/**");
    }

    [Fact]
    public void DefaultSdkExcludes_ContainsBinPattern()
    {
        SourceFileDiscovery.DefaultSdkExcludes.ShouldContain("**/bin/**");
    }

    #endregion

    private static void SkipIfPathDoesNotExist(string path)
    {
        if (!Directory.Exists(path))
        {
            Assert.Fail($"Test project path does not exist: {path}. Ensure test fixtures are created.");
        }
    }
}
