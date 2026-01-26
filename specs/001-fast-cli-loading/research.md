# Research: Fast CLI Project Loading

**Feature**: 001-fast-cli-loading
**Date**: 2026-01-23
**Status**: Complete

## Research Tasks

### 1. SDK-Style vs Legacy Project File Parsing

**Question**: How to reliably detect and parse both SDK-style and legacy .csproj formats?

**Research Findings**:

SDK-style projects are identified by:
- `<Project Sdk="Microsoft.NET.Sdk">` or `<Project Sdk="Microsoft.NET.Sdk.Web">` root element
- Implicit `**/*.cs` globbing (unless `EnableDefaultCompileItems` is false)
- Support for `<Compile Include="..." />`, `<Compile Remove="..." />`, `<Compile Update="..." />`

Legacy projects are identified by:
- `<Project>` root with MSBuild Tools version attribute
- No Sdk attribute
- Explicit `<Compile Include="..." />` for every source file
- May use wildcards but requires explicit definition

**Decision**: Parse as XML, check for `Sdk` attribute on root `<Project>` element. If present, use SDK-style parsing with default globs; if absent, use legacy parsing with explicit includes only.

**Rationale**: This is the standard detection method used by MSBuild itself and is reliable across all .NET project types.

**Alternatives Considered**:
- Use MSBuild APIs for project evaluation: Rejected because it requires MSBuild installation and is slow
- Regex-based detection: Rejected because XML parsing is more robust and already needed

---

### 2. Glob Pattern Implementation

**Question**: How to implement glob pattern matching for SDK-style project file includes/excludes?

**Research Findings**:

`Microsoft.Extensions.FileSystemGlobbing` (already in project) provides:
- `Matcher` class with `AddInclude()` and `AddExclude()` methods
- `Execute(DirectoryInfoWrapper root)` returns matching files
- Supports patterns: `**/*.cs`, `**/obj/**`, `*.cs`

SDK-style default patterns:
- Include: `**/*.cs`
- Exclude: `**/obj/**`, `**/bin/**`

Pattern priority in .csproj:
1. Default includes (if `EnableDefaultCompileItems` != false)
2. Explicit `<Compile Include="..." />` additions
3. Explicit `<Compile Remove="..." />` removals
4. Explicit `<Compile Update="..." />` modifications (no file changes, just metadata)

**Decision**: Use `Microsoft.Extensions.FileSystemGlobbing.Matcher` for all glob operations. Apply patterns in MSBuild-defined order: defaults first, then explicit includes, then removes.

**Rationale**: Already a project dependency, well-tested, handles edge cases (symlinks, case sensitivity).

**Alternatives Considered**:
- Custom glob implementation: Rejected as unnecessary when library exists
- Direct filesystem enumeration: Rejected because it doesn't handle patterns correctly

---

### 3. Runtime Assembly Reference Resolution

**Question**: How to get .NET runtime assembly references without MSBuild evaluation?

**Research Findings**:

For .NET Core/.NET 5+:
- `AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")` returns semicolon-separated list of runtime assemblies
- This is the same mechanism used by the runtime itself
- Works for any .NET runtime (including the CLI's own runtime)

For .NET Framework targeting (when CLI runs on .NET 8 but analyzes .NET Framework code):
- Use reference assemblies from NuGet packages or SDK
- Path pattern: `~/.nuget/packages/netstandard.library/2.0.3/build/netstandard2.0/ref/`
- Or use `Microsoft.NETFramework.ReferenceAssemblies` package

**Decision**: Use `AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")` as primary source. For template code model purposes, the exact framework references aren't critical - Roslyn only needs type definitions to resolve symbols.

**Rationale**: Templates access type information (names, properties, methods) but don't execute code. The CLI's runtime assemblies provide sufficient type information for 95%+ of use cases.

**Alternatives Considered**:
- Parse .csproj for TargetFramework and resolve framework-specific refs: Adds complexity for marginal benefit
- Require user to specify framework path: Poor UX

---

### 4. Solution File Parsing

**Question**: How to parse .sln files to extract project references and build order?

**Research Findings**:

.sln file format:
```
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ProjectName", "path\to\project.csproj", "{GUID}"
EndProject
```

Key patterns:
- Project type GUID `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}` = C# project
- Project type GUID `{2150E333-8FDC-42A3-9474-1A3956D46DE8}` = Solution folder (skip)
- Relative paths from solution directory

Project dependencies are defined in GlobalSection:
```
GlobalSection(ProjectDependencies) = postSolution
    {GUID} = {GUID}
EndGlobalSection
```

**Decision**: Use regex-based parsing for simplicity. Extract project paths, filter for .csproj files, resolve relative paths.

**Rationale**: .sln format is stable (unchanged for 15+ years), regex is sufficient, no need for full parser.

**Alternatives Considered**:
- Use Microsoft.Build.Construction.SolutionFile: Requires MSBuild APIs, adds dependency
- Full parser with AST: Overkill for extracting project paths

---

### 5. Project Reference Resolution

**Question**: How to resolve `<ProjectReference>` elements for cross-project type information?

**Research Findings**:

Project references in .csproj:
```xml
<ProjectReference Include="..\OtherProject\OtherProject.csproj" />
```

For Roslyn compilation:
- Each project compiles to its own `CSharpCompilation`
- Cross-project references use `MetadataReference.CreateFromFile(dllPath)` or `CompilationReference`
- With `AdhocWorkspace`, can use `project.AddProjectReference()`

Build order matters:
- Referenced projects must compile before referencing project
- Topological sort based on project reference graph

**Decision**: Build project dependency graph from `<ProjectReference>` elements. Compile in topological order. Add each project's compilation as a reference to dependent projects.

**Rationale**: This mirrors how MSBuild resolves references and ensures type information flows correctly.

**Alternatives Considered**:
- Compile all projects in parallel: Would fail due to missing references
- Use compiled DLLs from output: Requires prior build, defeats purpose

---

### 6. Fallback Strategy

**Question**: When should fast loading fall back to Buildalyzer, and how to implement cleanly?

**Research Findings**:

Fallback triggers:
1. Project file parse errors (malformed XML)
2. Unsupported MSBuild features (conditional compilation, imports)
3. Missing source files
4. Compilation errors that prevent semantic model

Implementation patterns:
- Try/catch with specific exception types
- Timeout-based (if fast loading exceeds threshold)
- Explicit user flag (--force-slow, --no-fallback)

**Decision**: Try fast loading first. On any exception or compilation failure, fall back to Buildalyzer. Log warning with reason. Support `--no-fallback` flag to fail fast during development.

**Rationale**: User experience prioritizes working generation over speed. Verbose logging helps diagnose issues.

**Alternatives Considered**:
- Parallel loading (race): Wastes resources, complicates error handling
- User choice only: Poor default experience

---

### 7. Lazy Semantic Model Loading

**Question**: How to defer semantic model creation for performance?

**Research Findings**:

Current flow:
1. Load all source files into workspace
2. Get compilation for project
3. For each file, `document.GetSemanticModelAsync()` - this is expensive

Roslyn lazy patterns:
- `SyntaxTree` parsing is fast (~1ms per file)
- `SemanticModel` requires full compilation binding (~10-100ms per file)
- `Compilation` is incrementally built but binding is on-demand

**Decision**: Create `CSharpSyntaxTree` for all files immediately. Create `CSharpCompilation` with all trees. Defer `GetSemanticModel()` calls until metadata provider requests specific file.

**Rationale**: Syntax trees are needed for file filtering anyway. Compilation creation is fast. Semantic model is only needed when templates actually access type information.

**Alternatives Considered**:
- Fully lazy (don't create compilation): Would defer too much, unpredictable latency
- Pre-compute all semantic models: Defeats the purpose

---

## Summary of Decisions

| Area | Decision | Impact |
|------|----------|--------|
| Project Detection | Check `Sdk` attribute on `<Project>` element | Reliable SDK/legacy differentiation |
| Glob Matching | Use `Microsoft.Extensions.FileSystemGlobbing` | No new dependencies |
| Reference Resolution | Use `AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")` | Works cross-platform |
| Solution Parsing | Regex-based extraction | Simple, maintainable |
| Project References | Topological sort, compile in order | Correct type resolution |
| Fallback | Try fast first, Buildalyzer on failure | Best UX |
| Lazy Loading | Defer semantic model, not compilation | Balanced performance |

## Open Questions

None - all technical questions resolved.

## References

- [MSBuild SDK-style projects](https://docs.microsoft.com/en-us/dotnet/core/project-sdk/overview)
- [Microsoft.Extensions.FileSystemGlobbing](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.filesystemglobbing)
- [Roslyn Workspaces](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/work-with-workspace)
- [.NET Runtime TRUSTED_PLATFORM_ASSEMBLIES](https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md)
