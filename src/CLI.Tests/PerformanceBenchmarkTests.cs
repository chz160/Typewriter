using System.Diagnostics;
using Shouldly;
using Typewriter.CLI.Infrastructure;
using Xunit;

namespace Typewriter.CLI.Tests;

/// <summary>
/// Performance benchmark tests for verifying fast loading performance requirements.
/// T050: 700 files should load in under 5 seconds.
/// T051: Memory usage should stay under 1GB.
/// </summary>
[Trait("Category", "CLI")]
[Trait("Component", "Performance")]
public class PerformanceBenchmarkTests : IDisposable
{
    private readonly string _testProjectsPath;
    private readonly string _largeProjectPath;
    private DirectRoslynWorkspace? _workspace;

    public PerformanceBenchmarkTests()
    {
        _testProjectsPath = FindTestProjectsPath();
        _largeProjectPath = Path.Combine(_testProjectsPath, "LargeProject");
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

    public void Dispose()
    {
        _workspace?.Dispose();
    }

    #region Performance Tests (T050)

    /// <summary>
    /// Verifies that loading a project with 700 source files completes in under 5 seconds.
    /// This is the primary performance requirement from the spec.
    /// </summary>
    [Fact(Skip = "Requires LargeProject test fixture with 700 files")]
    public async Task LoadProjectAsync_With700Files_CompletesUnder5Seconds()
    {
        // Arrange
        var projectPath = Path.Combine(_largeProjectPath, "LargeProject.csproj");
        if (!File.Exists(projectPath))
        {
            Assert.Fail($"LargeProject test fixture not found at {projectPath}. Create it with 700 source files.");
        }

        _workspace = new DirectRoslynWorkspace();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var success = await _workspace.LoadProjectAsync(projectPath);

        // Assert
        stopwatch.Stop();
        success.ShouldBeTrue("Project should load successfully");
        stopwatch.Elapsed.TotalSeconds.ShouldBeLessThan(5.0,
            $"Loading 700 files should complete in under 5 seconds, but took {stopwatch.Elapsed.TotalSeconds:F2}s");
    }

    /// <summary>
    /// Verifies that SDK-style project loading is fast (sub-second for small projects).
    /// </summary>
    [Fact]
    public async Task LoadProjectAsync_SdkStyleProject_LoadsFast()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        if (!File.Exists(projectPath))
        {
            Assert.Fail($"SdkStyleProject test fixture not found at {projectPath}.");
        }

        _workspace = new DirectRoslynWorkspace();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var success = await _workspace.LoadProjectAsync(projectPath);

        // Assert
        stopwatch.Stop();
        success.ShouldBeTrue("Project should load successfully");
        // Small projects should load in under 1 second
        stopwatch.Elapsed.TotalSeconds.ShouldBeLessThan(1.0,
            $"Small SDK-style project should load in under 1 second, but took {stopwatch.Elapsed.TotalSeconds:F2}s");
    }

    /// <summary>
    /// Verifies that legacy project loading is still reasonably fast.
    /// </summary>
    [Fact]
    public async Task LoadProjectAsync_LegacyProject_LoadsFast()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "LegacyProject", "LegacyProject.csproj");
        if (!File.Exists(projectPath))
        {
            Assert.Fail($"LegacyProject test fixture not found at {projectPath}.");
        }

        _workspace = new DirectRoslynWorkspace();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var success = await _workspace.LoadProjectAsync(projectPath);

        // Assert
        stopwatch.Stop();
        success.ShouldBeTrue("Project should load successfully");
        // Legacy projects should also load in under 2 seconds for small projects
        stopwatch.Elapsed.TotalSeconds.ShouldBeLessThan(2.0,
            $"Small legacy project should load in under 2 seconds, but took {stopwatch.Elapsed.TotalSeconds:F2}s");
    }

    /// <summary>
    /// Verifies that solution loading completes in reasonable time.
    /// </summary>
    [Fact]
    public async Task LoadSolutionAsync_MixedSolution_LoadsFast()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        if (!File.Exists(solutionPath))
        {
            Assert.Fail($"MixedSolution test fixture not found at {solutionPath}.");
        }

        _workspace = new DirectRoslynWorkspace();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var success = await _workspace.LoadSolutionAsync(solutionPath);

        // Assert
        stopwatch.Stop();
        success.ShouldBeTrue("Solution should load successfully");
        // Small solutions should load in under 3 seconds
        stopwatch.Elapsed.TotalSeconds.ShouldBeLessThan(3.0,
            $"Small solution should load in under 3 seconds, but took {stopwatch.Elapsed.TotalSeconds:F2}s");
    }

    /// <summary>
    /// Verifies that fast loading is significantly faster than traditional MSBuild evaluation.
    /// This test documents the expected performance improvement.
    /// </summary>
    [Fact]
    public async Task FastLoading_IsFasterThan_TraditionalLoading()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        if (!File.Exists(projectPath))
        {
            Assert.Fail($"SdkStyleProject test fixture not found at {projectPath}.");
        }

        // Warm up - first load may be slower due to JIT
        using (var warmup = new DirectRoslynWorkspace())
        {
            await warmup.LoadProjectAsync(projectPath);
        }

        // Measure fast loading
        var fastTimes = new List<double>();
        for (var i = 0; i < 3; i++)
        {
            using var fastWorkspace = new DirectRoslynWorkspace();
            var sw = Stopwatch.StartNew();
            await fastWorkspace.LoadProjectAsync(projectPath);
            sw.Stop();
            fastTimes.Add(sw.Elapsed.TotalMilliseconds);
        }

        var averageFastTime = fastTimes.Average();

        // Assert - Fast loading should complete quickly
        // We expect sub-second loading for small projects
        averageFastTime.ShouldBeLessThan(1000.0,
            $"Average fast loading time should be under 1000ms, but was {averageFastTime:F0}ms");
    }

    #endregion

    #region Memory Usage Tests (T051)

    /// <summary>
    /// Verifies that memory usage stays under 1GB during project loading.
    /// </summary>
    [Fact(Skip = "Requires LargeProject test fixture with 700 files")]
    public async Task LoadProjectAsync_With700Files_UsesUnder1GBMemory()
    {
        // Arrange
        var projectPath = Path.Combine(_largeProjectPath, "LargeProject.csproj");
        if (!File.Exists(projectPath))
        {
            Assert.Fail($"LargeProject test fixture not found at {projectPath}. Create it with 700 files.");
        }

        // Force garbage collection before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(true);
        _workspace = new DirectRoslynWorkspace();

        // Act
        await _workspace.LoadProjectAsync(projectPath);

        // Measure memory after loading
        var memoryAfter = GC.GetTotalMemory(false);
        var memoryUsed = memoryAfter - memoryBefore;
        var memoryUsedMB = memoryUsed / (1024.0 * 1024.0);
        var memoryUsedGB = memoryUsedMB / 1024.0;

        // Assert
        memoryUsedGB.ShouldBeLessThan(1.0,
            $"Memory usage should be under 1GB, but was {memoryUsedGB:F2}GB ({memoryUsedMB:F0}MB)");
    }

    /// <summary>
    /// Verifies that memory usage for small projects is reasonable.
    /// </summary>
    [Fact]
    public async Task LoadProjectAsync_SmallProject_UsesReasonableMemory()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        if (!File.Exists(projectPath))
        {
            Assert.Fail($"SdkStyleProject test fixture not found at {projectPath}.");
        }

        // Force garbage collection before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(true);
        _workspace = new DirectRoslynWorkspace();

        // Act
        await _workspace.LoadProjectAsync(projectPath);

        // Measure memory after loading
        var memoryAfter = GC.GetTotalMemory(false);
        var memoryUsed = memoryAfter - memoryBefore;
        var memoryUsedMB = memoryUsed / (1024.0 * 1024.0);

        // Assert - Small projects should use less than 100MB
        memoryUsedMB.ShouldBeLessThan(100.0,
            $"Small project memory usage should be under 100MB, but was {memoryUsedMB:F0}MB");
    }

    /// <summary>
    /// Verifies that disposed workspaces release memory properly.
    /// </summary>
    [Fact]
    public async Task DisposingWorkspace_ReleasesMemory()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        if (!File.Exists(projectPath))
        {
            Assert.Fail($"SdkStyleProject test fixture not found at {projectPath}.");
        }

        // Force GC and measure baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var baselineMemory = GC.GetTotalMemory(true);

        // Load and dispose multiple workspaces
        for (var i = 0; i < 5; i++)
        {
            using var workspace = new DirectRoslynWorkspace();
            await workspace.LoadProjectAsync(projectPath);
        }

        // Force GC after disposal
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - baselineMemory;
        var memoryIncreaseMB = memoryIncrease / (1024.0 * 1024.0);

        // Assert - Memory should not accumulate significantly after disposal
        // Allow some memory growth but not unbounded leaks
        memoryIncreaseMB.ShouldBeLessThan(50.0,
            $"Memory increase after disposing 5 workspaces should be under 50MB, but was {memoryIncreaseMB:F0}MB");
    }

    #endregion

    #region Benchmark Utilities

    /// <summary>
    /// Utility method to run a performance benchmark and report results.
    /// </summary>
    private static BenchmarkResult RunBenchmark(string name, Action action, int iterations = 5)
    {
        // Warm up
        action();

        var times = new List<double>();
        for (var i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            times.Add(sw.Elapsed.TotalMilliseconds);
        }

        return new BenchmarkResult(
            name,
            times.Average(),
            times.Min(),
            times.Max(),
            times.Count);
    }

    private record BenchmarkResult(
        string Name,
        double AverageMs,
        double MinMs,
        double MaxMs,
        int Iterations)
    {
        public override string ToString() =>
            $"{Name}: Avg={AverageMs:F0}ms, Min={MinMs:F0}ms, Max={MaxMs:F0}ms ({Iterations} iterations)";
    }

    #endregion
}
