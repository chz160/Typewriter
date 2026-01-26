# Quickstart: Fast CLI Project Loading

**Feature**: 001-fast-cli-loading
**Date**: 2026-01-23
**Audience**: Developers implementing this feature

## Overview

This guide provides a quick reference for implementing the fast CLI project loading feature. It covers the key components, their responsibilities, and implementation order.

## Implementation Order

### Phase 1: Core Parsers (No dependencies)

1. **ProjectFileParser** - Parse .csproj XML
2. **SolutionFileParser** - Parse .sln text format
3. **SourceFileDiscovery** - Glob pattern matching

### Phase 2: Reference Resolution

4. **ReferenceResolver** - Get runtime assemblies

### Phase 3: Workspace Integration

5. **DirectRoslynWorkspace** - Fast Roslyn workspace
6. **CliRoslynWorkspace** - Add fallback orchestration

### Phase 4: Testing & Validation

7. Unit tests for each parser
8. Integration tests for full loading
9. Performance benchmarks

## Key Implementation Details

### ProjectFileParser

```csharp
// Detection: Check for Sdk attribute
var root = XDocument.Load(projectPath).Root;
bool isSdk = root?.Attribute("Sdk") != null;

// SDK-style defaults
var defaultIncludes = new[] { "**/*.cs" };
var defaultExcludes = new[] { "**/obj/**", "**/bin/**" };

// Parse explicit items
var compileIncludes = root.Descendants("Compile")
    .Where(e => e.Attribute("Include") != null)
    .Select(e => e.Attribute("Include")!.Value);
```

### SolutionFileParser

```csharp
// Regex for project entries
var projectPattern = new Regex(
    @"Project\(""\{[^}]+\}""\)\s*=\s*""([^""]+)""\s*,\s*""([^""]+)""\s*,\s*""\{([^}]+)\}""",
    RegexOptions.Compiled);

// C# project type GUID
const string CSharpProjectTypeGuid = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";
```

### SourceFileDiscovery

```csharp
// Use Microsoft.Extensions.FileSystemGlobbing
var matcher = new Matcher();
matcher.AddIncludePatterns(includePatterns);
matcher.AddExcludePatterns(excludePatterns);

var directoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(projectDirectory));
var result = matcher.Execute(directoryInfo);
var files = result.Files.Select(f => Path.Combine(projectDirectory, f.Path));
```

### ReferenceResolver

```csharp
// Get runtime assemblies
var assemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
var paths = assemblies?.Split(Path.PathSeparator) ?? Array.Empty<string>();
var references = paths
    .Where(p => File.Exists(p))
    .Select(p => MetadataReference.CreateFromFile(p));
```

### DirectRoslynWorkspace

```csharp
// Create compilation without MSBuild
var syntaxTrees = sourceFiles.Select(f =>
    CSharpSyntaxTree.ParseText(
        File.ReadAllText(f),
        CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
        path: f));

var compilation = CSharpCompilation.Create(
    assemblyName,
    syntaxTrees,
    references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        .WithNullableContextOptions(NullableContextOptions.Enable));

// Add to AdhocWorkspace
var workspace = new AdhocWorkspace();
var projectInfo = ProjectInfo.Create(
    ProjectId.CreateNewId(),
    VersionStamp.Create(),
    name: projectName,
    assemblyName: assemblyName,
    language: LanguageNames.CSharp);
// ... add documents
```

### Fallback Pattern

```csharp
public async Task<LoadingResult> LoadProjectAsync(string projectPath, CancellationToken ct)
{
    var stopwatch = Stopwatch.StartNew();

    try
    {
        // Try fast loading first
        var result = await TryFastLoadAsync(projectPath, ct);
        if (result.Success)
        {
            return new LoadingResult
            {
                Success = true,
                UsedFallback = false,
                LoadTime = stopwatch.Elapsed
            };
        }
    }
    catch (Exception ex)
    {
        _diagnostics.AddWarning($"Fast loading failed: {ex.Message}");
    }

    // Fall back to Buildalyzer
    _output?.Warning("Falling back to Buildalyzer (slower)...");
    return await LoadWithBuildalyzerAsync(projectPath, ct, stopwatch);
}
```

## Testing Checklist

### Unit Tests (All Passing)

- [x] ProjectFileParser detects SDK-style vs legacy
- [x] ProjectFileParser extracts Compile includes/excludes
- [x] SolutionFileParser extracts C# project paths
- [x] SolutionFileParser ignores solution folders
- [x] SourceFileDiscovery respects include patterns
- [x] SourceFileDiscovery respects exclude patterns
- [x] SourceFileDiscovery excludes obj/bin by default
- [x] ReferenceResolver returns valid assemblies

### Integration Tests (All Passing)

- [x] Load SDK-style project with default globs
- [x] Load legacy project with explicit includes
- [x] Load solution with multiple projects
- [x] Load solution with project references
- [x] Load project with source files outside project directory
- [x] Fallback triggers on parse failure
- [x] Fallback gracefully handles malformed projects
- [x] Performance: Small projects < 100ms

### Compatibility Tests (All Passing)

- [x] Class metadata matches between fast and slow loading
- [x] Attribute metadata matches
- [x] Generic type metadata matches
- [x] Nullability metadata matches
- [x] Inheritance metadata matches

### Test Summary

**Total Tests**: 359
**Passed**: 357
**Skipped**: 2 (require 700-file test fixture)
**Failed**: 0

### Test Fixtures

Create minimal test projects under `src/Tests/TestProjects/`:

```text
SdkStyleProject/
├── SdkStyleProject.csproj
├── Class1.cs
└── SubFolder/
    └── Class2.cs

LegacyProject/
├── LegacyProject.csproj
├── Class1.cs
└── Class2.cs

MixedSolution/
├── MixedSolution.sln
├── SdkProject/
│   └── SdkProject.csproj
└── LegacyProject/
    └── LegacyProject.csproj
```

## Performance Targets

| Metric | Target | Actual Result |
|--------|--------|---------------|
| SDK-style project (small) | < 1 second | **63ms** |
| Legacy project (small) | < 2 seconds | **56ms** |
| Mixed solution (small) | < 3 seconds | **81ms** |
| Single project (700 files) | < 5 seconds | Pending (requires test fixture) |
| Solution (50 projects) | < 30 seconds | Pending (requires test fixture) |
| Memory (small project) | < 100 MB | Verified under 100MB |
| Memory after dispose | No leak | Verified under 50MB increase after 5 loads |

### How to Measure

- **Load time**: `Stopwatch` around `LoadProjectAsync` / `LoadSolutionAsync`
- **Memory**: `GC.GetTotalMemory()` before/after loading
- **Verbose output**: Use `--verbose` flag to see timing breakdown

## Common Pitfalls

1. **Don't block on async**: Use `await`, not `.Result` or `.GetAwaiter().GetResult()` in new code
2. **Handle path separators**: Use `Path.Combine()` and `Path.DirectorySeparatorChar` for cross-platform
3. **Normalize paths**: Always use `Path.GetFullPath()` before comparison
4. **Check file existence**: Source files may be deleted after project parse
5. **Dispose workspace**: `AdhocWorkspace` implements `IDisposable`

## Files to Create

| File | Responsibility |
|------|----------------|
| `src/CLI/Infrastructure/ProjectFileParser.cs` | Parse .csproj XML |
| `src/CLI/Infrastructure/SolutionFileParser.cs` | Parse .sln text |
| `src/CLI/Infrastructure/SourceFileDiscovery.cs` | Glob matching |
| `src/CLI/Infrastructure/ReferenceResolver.cs` | Runtime refs |
| `src/CLI/Infrastructure/DirectRoslynWorkspace.cs` | Fast workspace |
| `src/Tests/CLI/ProjectFileParserTests.cs` | Unit tests |
| `src/Tests/CLI/SolutionFileParserTests.cs` | Unit tests |
| `src/Tests/CLI/SourceFileDiscoveryTests.cs` | Unit tests |
| `src/Tests/CLI/DirectRoslynWorkspaceTests.cs` | Integration tests |

## Files to Modify

| File | Changes |
|------|---------|
| `src/CLI/Infrastructure/CliRoslynWorkspace.cs` | Add fallback orchestration |
| `src/CLI/Commands/GenerateCommand.cs` | Add timing output with --verbose |
