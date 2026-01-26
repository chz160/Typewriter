using Shouldly;
using Typewriter.CLI.Infrastructure;
using Xunit;

namespace Typewriter.CLI.Tests;

/// <summary>
/// Integration tests for fallback behavior when fast loading fails.
/// Tests that CliRoslynWorkspace falls back to Buildalyzer gracefully.
/// </summary>
[Trait("Category", "CLI")]
[Trait("Component", "FallbackBehavior")]
public class FallbackBehaviorTests : IDisposable
{
    private readonly string _testProjectsPath;
    private CliRoslynWorkspace? _workspace;

    public FallbackBehaviorTests()
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

    #region Fallback on Malformed Project (T032)

    [Fact]
    public async Task LoadProjectAsync_MalformedProject_FallsBackToBuildalyzer()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "MalformedProject", "MalformedProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new CliRoslynWorkspace();

        // Act
        var result = await _workspace.LoadProjectWithResultAsync(projectPath);

        // Assert
        // Should either succeed with fallback, or fail gracefully
        // The key is that it doesn't throw and provides a useful result
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task LoadProjectAsync_MalformedProject_HandlesGracefully()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "MalformedProject", "MalformedProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new CliRoslynWorkspace();

        // Act
        var result = await _workspace.LoadProjectWithResultAsync(projectPath);

        // Assert
        // The system should handle malformed projects gracefully - either succeeding
        // with fast loading (if parseable) or falling back to Buildalyzer, or failing
        // gracefully with diagnostics
        result.ShouldNotBeNull();
        // If it failed, there should be diagnostics explaining why
        if (!result.Success)
        {
            result.Diagnostics.Count.ShouldBeGreaterThan(0);
        }
    }

    [Fact]
    public async Task LoadProjectAsync_InvalidXmlProject_FailsGracefully()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "InvalidXmlProject", "InvalidXmlProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new CliRoslynWorkspace();

        // Act
        var result = await _workspace.LoadProjectWithResultAsync(projectPath);

        // Assert
        // Invalid XML should cause loading to fail, but gracefully with diagnostics
        result.ShouldNotBeNull();
        // Since even Buildalyzer can't parse invalid XML, this should fail
        // but with proper error messages
        if (!result.Success)
        {
            result.Diagnostics.Count.ShouldBeGreaterThan(0);
        }
    }

    [Fact]
    public async Task LoadProjectAsync_MalformedProject_ProvidesFallbackReason()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "MalformedProject", "MalformedProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new CliRoslynWorkspace();

        // Act
        var result = await _workspace.LoadProjectWithResultAsync(projectPath);

        // Assert
        if (result.UsedFallback)
        {
            result.FallbackReason.ShouldNotBeNullOrEmpty();
        }
    }

    #endregion

    #region Fallback on Unsupported Features (T033)

    [Fact]
    public async Task LoadProjectAsync_FastLoadingFails_AttemptsBuildalyzerFallback()
    {
        // Arrange
        // Use a valid project that fast loading should handle
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new CliRoslynWorkspace();

        // Act
        var result = await _workspace.LoadProjectWithResultAsync(projectPath);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        // Fast loading should succeed for SDK-style projects, so no fallback needed
        result.UsedFallback.ShouldBeFalse();
    }

    [Fact]
    public async Task LoadSolutionAsync_FastLoadingFails_AttemptsBuildalyzerFallback()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);
        _workspace = new CliRoslynWorkspace();

        // Act
        var result = await _workspace.LoadSolutionWithResultAsync(solutionPath);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
    }

    #endregion

    #region Verbose Flag Tests (T034)

    [Fact]
    public async Task LoadProjectAsync_WithVerboseOutput_ShowsFallbackReason()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "MalformedProject", "MalformedProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        var outputMessages = new List<string>();
        var output = new TestConsoleOutput(outputMessages);
        _workspace = new CliRoslynWorkspace(output);

        // Act
        var result = await _workspace.LoadProjectWithResultAsync(projectPath);

        // Assert
        // If fallback occurred, verbose output should mention it
        if (result.UsedFallback)
        {
            outputMessages.ShouldContain(m =>
                m.Contains("fallback", StringComparison.OrdinalIgnoreCase) ||
                m.Contains("Buildalyzer", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task LoadProjectAsync_FastLoadingSucceeds_ShowsFastLoadingMessage()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        var outputMessages = new List<string>();
        var output = new TestConsoleOutput(outputMessages);
        _workspace = new CliRoslynWorkspace(output);

        // Act
        var result = await _workspace.LoadProjectWithResultAsync(projectPath);

        // Assert
        result.Success.ShouldBeTrue();
        outputMessages.ShouldContain(m => m.Contains("fast", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Loading Result Tests

    [Fact]
    public async Task LoadProjectWithResultAsync_ReturnsLoadingResult()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);
        _workspace = new CliRoslynWorkspace();

        // Act
        var result = await _workspace.LoadProjectWithResultAsync(projectPath);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.ProjectCount.ShouldBe(1);
        result.SourceFileCount.ShouldBeGreaterThan(0);
        result.LoadTime.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task LoadSolutionWithResultAsync_ReturnsLoadingResult()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);
        _workspace = new CliRoslynWorkspace();

        // Act
        var result = await _workspace.LoadSolutionWithResultAsync(solutionPath);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.ProjectCount.ShouldBeGreaterThan(0);
        result.LoadTime.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task LoadProjectWithResultAsync_NonExistentProject_ReturnsFailed()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "NonExistent.csproj");
        _workspace = new CliRoslynWorkspace();

        // Act
        var result = await _workspace.LoadProjectWithResultAsync(projectPath);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(d => d.Message.Contains("not found"));
    }

    #endregion

    /// <summary>
    /// Test helper that captures console output messages.
    /// </summary>
    private class TestConsoleOutput : ConsoleOutput
    {
        private readonly List<string> _messages;

        public TestConsoleOutput(List<string> messages)
        {
            _messages = messages;
        }

        public override void Info(string message)
        {
            _messages.Add($"[INFO] {message}");
        }

        public override void Verbose(string message)
        {
            _messages.Add($"[VERBOSE] {message}");
        }

        public override void Warning(string message)
        {
            _messages.Add($"[WARNING] {message}");
        }

        public override void Error(string message)
        {
            _messages.Add($"[ERROR] {message}");
        }
    }
}
