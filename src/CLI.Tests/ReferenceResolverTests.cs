using Microsoft.CodeAnalysis;
using Shouldly;
using Typewriter.CLI.Infrastructure;
using Xunit;

namespace Typewriter.CLI.Tests;

/// <summary>
/// Unit tests for <see cref="ReferenceResolver"/>.
/// Tests runtime reference resolution for Roslyn compilation.
/// </summary>
[Trait("Category", "CLI")]
[Trait("Component", "ReferenceResolver")]
public class ReferenceResolverTests
{
    private readonly ReferenceResolver _resolver;

    public ReferenceResolverTests()
    {
        _resolver = new ReferenceResolver();
    }

    #region GetRuntimeReferences Tests

    [Fact]
    public void GetRuntimeReferences_ReturnsNonEmptyCollection()
    {
        // Act
        var references = _resolver.GetRuntimeReferences();

        // Assert
        references.ShouldNotBeEmpty();
    }

    [Fact]
    public void GetRuntimeReferences_ReturnsMetadataReferences()
    {
        // Act
        var references = _resolver.GetRuntimeReferences();

        // Assert
        references.ShouldAllBe(r => r is PortableExecutableReference);
    }

    [Fact]
    public void GetRuntimeReferences_IncludesCoreAssemblies()
    {
        // Act
        var references = _resolver.GetRuntimeReferences().ToList();
        var referencePaths = references
            .OfType<PortableExecutableReference>()
            .Select(r => r.FilePath ?? string.Empty)
            .ToList();

        // Assert - should include System.Runtime or mscorlib at minimum
        var hasCoreRuntime = referencePaths.Any(p =>
            p.Contains("System.Runtime", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("mscorlib", StringComparison.OrdinalIgnoreCase));

        hasCoreRuntime.ShouldBeTrue();
    }

    [Fact]
    public void GetRuntimeReferences_IncludesSystemCollections()
    {
        // Act
        var references = _resolver.GetRuntimeReferences().ToList();
        var referencePaths = references
            .OfType<PortableExecutableReference>()
            .Select(r => r.FilePath ?? string.Empty)
            .ToList();

        // Assert
        referencePaths.ShouldContain(p => p.Contains("System.Collections", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRuntimeReferences_IncludesSystemLinq()
    {
        // Act
        var references = _resolver.GetRuntimeReferences().ToList();
        var referencePaths = references
            .OfType<PortableExecutableReference>()
            .Select(r => r.FilePath ?? string.Empty)
            .ToList();

        // Assert
        referencePaths.ShouldContain(p => p.Contains("System.Linq", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRuntimeReferences_IsCached()
    {
        // Act
        var firstCall = _resolver.GetRuntimeReferences();
        var secondCall = _resolver.GetRuntimeReferences();

        // Assert - should return same instance due to Lazy<T> caching
        ReferenceEquals(firstCall, secondCall).ShouldBeTrue();
    }

    [Fact]
    public void GetRuntimeReferences_AllReferencesHaveValidPaths()
    {
        // Act
        var references = _resolver.GetRuntimeReferences().ToList();

        // Assert
        foreach (var reference in references.OfType<PortableExecutableReference>())
        {
            reference.FilePath.ShouldNotBeNull();
            File.Exists(reference.FilePath).ShouldBeTrue($"Reference path should exist: {reference.FilePath}");
        }
    }

    #endregion

    #region GetProjectReferences Tests

    [Fact]
    public void GetProjectReferences_ReturnsRuntimeReferences()
    {
        // Arrange
        var projectInfo = new Infrastructure.Models.ProjectInfo(
            projectPath: "Test.csproj",
            isSdkStyle: true,
            targetFramework: "net8.0");

        // Act
        var references = _resolver.GetProjectReferences(projectInfo);

        // Assert
        references.ShouldNotBeEmpty();
        references.SequenceEqual(_resolver.GetRuntimeReferences()).ShouldBeTrue();
    }

    #endregion

    #region GetRuntimeReferenceCount Tests

    [Fact]
    public void GetRuntimeReferenceCount_ReturnsPositiveCount()
    {
        // Act
        var count = _resolver.GetRuntimeReferenceCount();

        // Assert
        count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GetRuntimeReferenceCount_MatchesEnumerableCount()
    {
        // Act
        var countMethod = _resolver.GetRuntimeReferenceCount();
        var enumerableCount = _resolver.GetRuntimeReferences().Count();

        // Assert
        countMethod.ShouldBe(enumerableCount);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetRuntimeReferences_HandlesMultipleConcurrentCalls()
    {
        // Arrange
        var tasks = new List<Task<IEnumerable<MetadataReference>>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => _resolver.GetRuntimeReferences()));
        }
        var results = await Task.WhenAll(tasks);

        // Assert - all should return same instance
        var firstResult = results[0];
        results.ShouldAllBe(r => ReferenceEquals(r, firstResult));
    }

    [Fact]
    public void GetRuntimeReferences_ReferencesAreReadable()
    {
        // Act
        var references = _resolver.GetRuntimeReferences().ToList();

        // Assert - verify at least some references can provide metadata
        var peReferences = references.OfType<PortableExecutableReference>().Take(5);
        foreach (var reference in peReferences)
        {
            // Getting the metadata should not throw
            var metadata = reference.GetMetadata();
            metadata.ShouldNotBeNull();
        }
    }

    #endregion
}
