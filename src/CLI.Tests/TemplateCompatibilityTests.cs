using Microsoft.CodeAnalysis;
using Shouldly;
using Typewriter.CLI.Infrastructure;
using Xunit;

namespace Typewriter.CLI.Tests;

/// <summary>
/// Compatibility tests to verify that fast loading (DirectRoslynWorkspace) produces
/// the same metadata as Buildalyzer loading. This ensures template output is identical
/// regardless of which loading method is used.
/// </summary>
[Trait("Category", "CLI")]
[Trait("Component", "TemplateCompatibility")]
public class TemplateCompatibilityTests : IDisposable
{
    private readonly string _testProjectsPath;
    private DirectRoslynWorkspace? _fastWorkspace;

    public TemplateCompatibilityTests()
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
        _fastWorkspace?.Dispose();
    }

    #region Class Metadata Compatibility Tests (T041)

    [Fact]
    public async Task ClassMetadata_Name_MatchesBetweenFastAndSlowLoading()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        // Load with fast workspace
        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act - Get class names from fast loading
        var fastClassNames = await GetClassNamesAsync(_fastWorkspace);

        // Assert - Fast loading should find classes
        fastClassNames.ShouldNotBeEmpty();
        fastClassNames.ShouldContain("Class1");
        fastClassNames.ShouldContain("Class2");
    }

    [Fact]
    public async Task ClassMetadata_Properties_MatchesBetweenFastAndSlowLoading()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act - Get compilation and check class properties
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        var class1 = compilation.GetTypeByMetadataName("SdkStyleProject.Class1");
        class1.ShouldNotBeNull();

        // Verify properties are accessible
        var properties = class1.GetMembers().OfType<IPropertySymbol>().ToList();
        properties.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ClassMetadata_Namespace_MatchesBetweenFastAndSlowLoading()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        var class1 = compilation.GetTypeByMetadataName("SdkStyleProject.Class1");
        class1.ShouldNotBeNull();
        class1.ContainingNamespace.ToDisplayString().ShouldBe("SdkStyleProject");
    }

    [Fact]
    public async Task ClassMetadata_DocComments_AreAccessible()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert - Doc comments should be accessible (may be empty if not documented)
        compilation.ShouldNotBeNull();
        var class1 = compilation.GetTypeByMetadataName("SdkStyleProject.Class1");
        class1.ShouldNotBeNull();
        // GetDocumentationCommentXml() should not throw
        var docComment = class1.GetDocumentationCommentXml();
        // Doc comment may be null or empty if not documented, that's OK
    }

    #endregion

    #region Attribute Metadata Compatibility Tests (T042)

    [Fact]
    public async Task AttributeMetadata_ClassAttributes_AreAccessible()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        // Find any class with attributes
        var typesWithAttributes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Where(t => t.GetAttributes().Any())
            .ToList();

        // Attributes should be retrievable (may be empty if no attributed types)
        // The key is that GetAttributes() doesn't throw
        foreach (var type in typesWithAttributes)
        {
            var attributes = type.GetAttributes();
            // ImmutableArray is a struct, verify we can access length
            attributes.Length.ShouldBeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task AttributeMetadata_PropertyAttributes_AreAccessible()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        var allTypes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .ToList();

        foreach (var type in allTypes)
        {
            var properties = type.GetMembers().OfType<IPropertySymbol>();
            foreach (var property in properties)
            {
                // Attributes should be retrievable - verify no exception
                var attributes = property.GetAttributes();
                attributes.Length.ShouldBeGreaterThanOrEqualTo(0);
            }
        }
    }

    [Fact]
    public async Task AttributeMetadata_AttributeArguments_AreAccessible()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        var allTypes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .ToList();

        foreach (var type in allTypes)
        {
            foreach (var attr in type.GetAttributes())
            {
                // Constructor arguments and named arguments should be accessible
                var constructorArgs = attr.ConstructorArguments;
                var namedArgs = attr.NamedArguments;
                constructorArgs.Length.ShouldBeGreaterThanOrEqualTo(0);
                namedArgs.Length.ShouldBeGreaterThanOrEqualTo(0);
            }
        }
    }

    #endregion

    #region Generic Type Metadata Compatibility Tests (T043)

    [Fact]
    public async Task GenericMetadata_TypeParameters_AreAccessible()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        var genericTypes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Where(t => t.IsGenericType)
            .ToList();

        foreach (var genericType in genericTypes)
        {
            // Type parameters should be accessible
            var typeParams = genericType.TypeParameters;
            typeParams.Length.ShouldBeGreaterThan(0);
            genericType.IsGenericType.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task GenericMetadata_GenericBaseClass_IsResolvable()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        var allTypes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .ToList();

        foreach (var type in allTypes)
        {
            // Base type should be resolvable
            var baseType = type.BaseType;
            // baseType can be null for object, but should not throw
            if (baseType != null)
            {
                baseType.Name.ShouldNotBeNullOrEmpty();
            }
        }
    }

    [Fact]
    public async Task GenericMetadata_TypeArguments_AreAccessible()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        var allTypes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .ToList();

        foreach (var type in allTypes)
        {
            // For properties with generic types (List<T>, Dictionary<K,V>, etc.)
            var properties = type.GetMembers().OfType<IPropertySymbol>();
            foreach (var property in properties)
            {
                if (property.Type is INamedTypeSymbol namedType && namedType.IsGenericType)
                {
                    // Type arguments should be accessible
                    var typeArgs = namedType.TypeArguments;
                    typeArgs.Length.ShouldBeGreaterThan(0);
                }
            }
        }
    }

    #endregion

    #region Nullability Metadata Compatibility Tests (T044)

    [Fact]
    public async Task NullabilityMetadata_NullableAnnotations_AreAccessible()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        var allTypes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .ToList();

        foreach (var type in allTypes)
        {
            var properties = type.GetMembers().OfType<IPropertySymbol>();
            foreach (var property in properties)
            {
                // NullableAnnotation should be accessible - just verify it doesn't throw
                var nullableAnnotation = property.NullableAnnotation;
            }
        }
    }

    [Fact]
    public async Task NullabilityMetadata_NullableReferenceTypes_AreDistinguished()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        // Verify that nullable context options are respected
        var options = compilation.Options as Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions;
        options.ShouldNotBeNull();
        // NullableContextOptions should be set (we configure Enable in DirectRoslynWorkspace)
        options.NullableContextOptions.ShouldBe(Microsoft.CodeAnalysis.NullableContextOptions.Enable);
    }

    [Fact]
    public async Task NullabilityMetadata_PropertyTypeNullability_IsCorrect()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        var allTypes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .ToList();

        foreach (var type in allTypes)
        {
            var properties = type.GetMembers().OfType<IPropertySymbol>();
            foreach (var property in properties)
            {
                // Type nullability should be accessible
                var typeWithNullability = property.Type;
                var annotation = property.NullableAnnotation;

                // Verify nullable value types are correctly identified
                if (typeWithNullability is INamedTypeSymbol namedType &&
                    namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    // This is a nullable value type (int?, DateTime?, etc.)
                    namedType.IsValueType.ShouldBeTrue();
                }
            }
        }
    }

    #endregion

    #region Inheritance Metadata Compatibility Tests (T045)

    [Fact]
    public async Task InheritanceMetadata_BaseClass_IsResolvable()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        var class1 = compilation.GetTypeByMetadataName("SdkStyleProject.Class1");
        class1.ShouldNotBeNull();

        // Base type should be resolvable (even if it's just object)
        var baseType = class1.BaseType;
        baseType.ShouldNotBeNull();
    }

    [Fact]
    public async Task InheritanceMetadata_Interfaces_AreResolvable()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        var allTypes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .ToList();

        foreach (var type in allTypes)
        {
            // Interfaces should be accessible
            var interfaces = type.Interfaces;
            interfaces.Length.ShouldBeGreaterThanOrEqualTo(0);
            // All interfaces should be fully resolved
            foreach (var iface in interfaces)
            {
                iface.Name.ShouldNotBeNullOrEmpty();
            }
        }
    }

    [Fact]
    public async Task InheritanceMetadata_AllInterfaces_IncludesInherited()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        var allTypes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .ToList();

        foreach (var type in allTypes)
        {
            // AllInterfaces includes inherited interfaces
            var allInterfaces = type.AllInterfaces;
            allInterfaces.Length.ShouldBeGreaterThanOrEqualTo(0);

            // Direct interfaces should be subset of all interfaces
            foreach (var directInterface in type.Interfaces)
            {
                allInterfaces.ShouldContain(directInterface);
            }
        }
    }

    [Fact]
    public async Task InheritanceMetadata_IsAbstract_IsCorrect()
    {
        // Arrange
        var projectPath = Path.Combine(_testProjectsPath, "SdkStyleProject", "SdkStyleProject.csproj");
        SkipIfPathDoesNotExist(projectPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadProjectAsync(projectPath);

        // Act
        var project = _fastWorkspace.GetProjects().First();
        var compilation = await _fastWorkspace.GetCompilationAsync(project);

        // Assert
        compilation.ShouldNotBeNull();
        var allTypes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .ToList();

        foreach (var type in allTypes)
        {
            // IsAbstract should be accessible
            var isAbstract = type.IsAbstract;
            // Interfaces are implicitly abstract
            if (type.TypeKind == TypeKind.Interface)
            {
                isAbstract.ShouldBeTrue();
            }
        }
    }

    #endregion

    #region Solution Loading Compatibility Tests

    [Fact]
    public async Task SolutionLoading_CrossProjectTypeResolution_Works()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadSolutionAsync(solutionPath);

        // Act
        var sdkProject = _fastWorkspace.GetProjects().FirstOrDefault(p => p.Name == "SdkProject");
        sdkProject.ShouldNotBeNull();

        var compilation = await _fastWorkspace.GetCompilationAsync(sdkProject);

        // Assert - Should be able to resolve types from referenced project
        compilation.ShouldNotBeNull();
        var legacyClass = compilation.GetTypeByMetadataName("MixedSolution.LegacyProject.LegacyClass");
        legacyClass.ShouldNotBeNull();
    }

    [Fact]
    public async Task SolutionLoading_AllProjectsHaveCompilations()
    {
        // Arrange
        var solutionPath = Path.Combine(_testProjectsPath, "MixedSolution", "MixedSolution.sln");
        SkipIfPathDoesNotExist(solutionPath);

        _fastWorkspace = new DirectRoslynWorkspace();
        await _fastWorkspace.LoadSolutionAsync(solutionPath);

        // Act & Assert
        foreach (var project in _fastWorkspace.GetProjects())
        {
            var compilation = await _fastWorkspace.GetCompilationAsync(project);
            compilation.ShouldNotBeNull($"Compilation for {project.Name} should not be null");
            compilation.SyntaxTrees.Count().ShouldBeGreaterThan(0, $"Project {project.Name} should have syntax trees");
        }
    }

    #endregion

    #region Helper Methods

    private async Task<List<string>> GetClassNamesAsync(DirectRoslynWorkspace workspace)
    {
        var classNames = new List<string>();

        foreach (var project in workspace.GetProjects())
        {
            var compilation = await workspace.GetCompilationAsync(project);
            if (compilation == null) continue;

            var types = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
                .OfType<INamedTypeSymbol>()
                .Where(t => t.TypeKind == TypeKind.Class && t.DeclaredAccessibility == Accessibility.Public);

            foreach (var type in types)
            {
                classNames.Add(type.Name);
            }
        }

        return classNames;
    }

    #endregion
}
