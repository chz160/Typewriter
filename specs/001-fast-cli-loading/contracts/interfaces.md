# Internal Interfaces: Fast CLI Project Loading

**Feature**: 001-fast-cli-loading
**Date**: 2026-01-23
**Status**: Complete

## Overview

These are internal implementation interfaces for the fast loading feature. They are not public API and may change between releases.

## Interfaces

### IProjectFileParser

Parses .csproj files to extract project metadata and source file patterns.

```csharp
/// <summary>
/// Parses .csproj files to extract metadata without MSBuild evaluation.
/// </summary>
public interface IProjectFileParser
{
    /// <summary>
    /// Parses a .csproj file and returns project information.
    /// </summary>
    /// <param name="projectPath">Absolute path to .csproj file.</param>
    /// <returns>Parsed project information, or null if parsing fails.</returns>
    ProjectInfo? Parse(string projectPath);

    /// <summary>
    /// Determines if a project file is SDK-style.
    /// </summary>
    /// <param name="projectPath">Absolute path to .csproj file.</param>
    /// <returns>True if SDK-style, false if legacy.</returns>
    bool IsSdkStyleProject(string projectPath);
}
```

---

### ISolutionFileParser

Parses .sln files to extract project references.

```csharp
/// <summary>
/// Parses .sln files to extract project entries.
/// </summary>
public interface ISolutionFileParser
{
    /// <summary>
    /// Parses a .sln file and returns solution information.
    /// </summary>
    /// <param name="solutionPath">Absolute path to .sln file.</param>
    /// <returns>Parsed solution information, or null if parsing fails.</returns>
    SolutionInfo? Parse(string solutionPath);

    /// <summary>
    /// Extracts C# project paths from a solution file.
    /// </summary>
    /// <param name="solutionPath">Absolute path to .sln file.</param>
    /// <returns>Enumerable of absolute paths to .csproj files.</returns>
    IEnumerable<string> GetCSharpProjectPaths(string solutionPath);
}
```

---

### ISourceFileDiscovery

Discovers source files based on project configuration.

```csharp
/// <summary>
/// Discovers source files for a project using glob patterns.
/// </summary>
public interface ISourceFileDiscovery
{
    /// <summary>
    /// Discovers source files for an SDK-style project.
    /// </summary>
    /// <param name="projectDirectory">Project root directory.</param>
    /// <param name="includePatterns">Additional include patterns (beyond defaults).</param>
    /// <param name="excludePatterns">Additional exclude patterns (beyond defaults).</param>
    /// <param name="removePatterns">Explicit remove patterns.</param>
    /// <returns>Source file discovery result.</returns>
    SourceFileResult DiscoverSdkStyleFiles(
        string projectDirectory,
        IEnumerable<string>? includePatterns = null,
        IEnumerable<string>? excludePatterns = null,
        IEnumerable<string>? removePatterns = null);

    /// <summary>
    /// Discovers source files for a legacy project with explicit includes.
    /// </summary>
    /// <param name="projectDirectory">Project root directory.</param>
    /// <param name="explicitIncludes">Explicit file include patterns.</param>
    /// <returns>Source file discovery result.</returns>
    SourceFileResult DiscoverLegacyFiles(
        string projectDirectory,
        IEnumerable<string> explicitIncludes);
}
```

---

### IReferenceResolver

Resolves assembly references for compilation.

```csharp
/// <summary>
/// Resolves assembly references for Roslyn compilation.
/// </summary>
public interface IReferenceResolver
{
    /// <summary>
    /// Gets .NET runtime assembly references from the current runtime.
    /// </summary>
    /// <returns>Enumerable of MetadataReference for runtime assemblies.</returns>
    IEnumerable<MetadataReference> GetRuntimeReferences();

    /// <summary>
    /// Gets assembly references from a project's reference assemblies.
    /// </summary>
    /// <param name="projectInfo">Project to get references for.</param>
    /// <returns>Enumerable of MetadataReference.</returns>
    IEnumerable<MetadataReference> GetProjectReferences(ProjectInfo projectInfo);
}
```

---

### IWorkspaceLoader

Orchestrates workspace loading with fallback support.

```csharp
/// <summary>
/// Loads workspaces with fast loading and fallback support.
/// </summary>
public interface IWorkspaceLoader
{
    /// <summary>
    /// Loads a solution into the workspace.
    /// </summary>
    /// <param name="solutionPath">Path to .sln file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Loading result with success status and diagnostics.</returns>
    Task<LoadingResult> LoadSolutionAsync(
        string solutionPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single project into the workspace.
    /// </summary>
    /// <param name="projectPath">Path to .csproj file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Loading result with success status and diagnostics.</returns>
    Task<LoadingResult> LoadProjectAsync(
        string projectPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all documents in the loaded workspace.
    /// </summary>
    /// <returns>Enumerable of Roslyn documents.</returns>
    IEnumerable<Document> GetAllDocuments();

    /// <summary>
    /// Gets the Roslyn workspace.
    /// </summary>
    Workspace Workspace { get; }
}
```

---

## Usage Patterns

### Typical Loading Flow

```csharp
// 1. Parse project/solution
var projectParser = new ProjectFileParser();
var projectInfo = projectParser.Parse(projectPath);

// 2. Discover source files
var discovery = new SourceFileDiscovery();
var files = projectInfo.IsSdkStyle
    ? discovery.DiscoverSdkStyleFiles(projectInfo.ProjectDirectory)
    : discovery.DiscoverLegacyFiles(projectInfo.ProjectDirectory, projectInfo.ExplicitIncludes);

// 3. Resolve references
var resolver = new ReferenceResolver();
var references = resolver.GetRuntimeReferences();

// 4. Create compilation
var syntaxTrees = files.IncludedFiles.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f)));
var compilation = CSharpCompilation.Create(
    projectInfo.AssemblyName,
    syntaxTrees,
    references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
```

### Fallback Pattern

```csharp
var loader = new WorkspaceLoader(output);

var result = await loader.LoadProjectAsync(projectPath);

if (result.UsedFallback)
{
    output.Warning($"Used fallback loading: {result.FallbackReason}");
}

if (result.Success)
{
    var documents = loader.GetAllDocuments();
    // Process templates...
}
```

## Design Decisions

1. **Interfaces over abstract classes**: Enables mocking in tests, follows existing codebase patterns
2. **Sync parsing, async loading**: File parsing is fast and synchronous; workspace loading is async for cancellation support
3. **Nullable returns**: Parse methods return null on failure rather than throwing, matching Roslyn patterns
4. **IEnumerable over IReadOnlyList**: Returns lazy sequences where appropriate for memory efficiency
