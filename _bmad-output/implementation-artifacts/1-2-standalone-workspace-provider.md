# Story 1.2: Standalone Workspace Provider

Status: done

## Story

As a **developer**,
I want **to specify a solution file path and have the CLI load it without Visual Studio**,
So that **I can use Typewriter from any editor or terminal**.

## Acceptance Criteria

1. **Given** a valid .sln file path **When** `CliMetadataProvider` is instantiated with that path **Then** the solution is loaded using Buildalyzer **And** an AdhocWorkspace is created with the solution's projects **And** no Visual Studio dependencies are required

2. **Given** the CLI executable exists **When** a user runs `typewriter generate --solution ./path/to/Solution.sln` **Then** the solution file is located and validated **And** the `CliMetadataProvider` loads the solution successfully

3. **Given** an invalid or non-existent solution path **When** a user runs `typewriter generate --solution ./invalid/path.sln` **Then** an error message is displayed indicating the file was not found **And** the CLI exits with exit code 2 (invalid args)

4. **Given** a solution file that cannot be parsed **When** `CliMetadataProvider` attempts to load it **Then** an error message is displayed with actionable information **And** the error does not crash the application (graceful handling) **And** the CLI exits with exit code 1 (generation failure)

**FRs Covered:** FR8, FR20

## Tasks / Subtasks

- [x] Task 1: Add Buildalyzer Dependencies to CLI Project (AC: #1)
  - [x] 1.1: Add project reference to `Buildalyzer\src\Buildalyzer\Buildalyzer.csproj`
  - [x] 1.2: Add project reference to `Buildalyzer\src\Buildalyzer.Workspaces\Buildalyzer.Workspaces.csproj`
  - [x] 1.3: Verify build succeeds with new references

- [x] Task 2: Create CliMetadataProvider Class (AC: #1, #2)
  - [x] 2.1: Create `src/CLI/Infrastructure/CliMetadataProvider.cs`
  - [x] 2.2: Implement `IMetadataProvider` interface with `GetFile(string path, Settings settings, Action<string[]> requestRender)` method
  - [x] 2.3: Add constructor that accepts solution path and creates `AnalyzerManager`
  - [x] 2.4: Implement workspace creation using `AnalyzerManager.GetWorkspace()` extension method
  - [x] 2.5: Use lazy loading pattern for workspace: `_workspace ?? (_workspace = LoadWorkspace())`

- [x] Task 3: Create CliFileMetadata Class (AC: #1)
  - [x] 3.1: Create `src/CLI/Infrastructure/CliFileMetadata.cs` to replace VS-dependent `RoslynFileMetadata`
  - [x] 3.2: Implement `IFileMetadata` interface
  - [x] 3.3: Use async pattern without `ThreadHelper` (use `.GetAwaiter().GetResult()` for sync-over-async)
  - [x] 3.4: Reuse the same metadata extraction logic from `RoslynFileMetadata`

- [x] Task 4: Implement Solution Path Validation (AC: #2, #3, #4)
  - [x] 4.1: Add path validation in `GenerateCommand.Execute()` before creating provider
  - [x] 4.2: Check if file exists using `System.IO.File.Exists()`
  - [x] 4.3: Validate file extension is `.sln`
  - [x] 4.4: Use `ConsoleOutput.Error()` for validation failures
  - [x] 4.5: Return exit code 2 for invalid/missing path

- [x] Task 5: Implement Error Handling for Solution Loading (AC: #4)
  - [x] 5.1: Wrap Buildalyzer calls in try-catch
  - [x] 5.2: Catch `InvalidOperationException`, `FileNotFoundException`, `Exception`
  - [x] 5.3: Format errors using compiler-style: `SolutionFile.sln: Error: <message>`
  - [x] 5.4: Return exit code 1 for solution parsing/loading failures

- [x] Task 6: Wire GenerateCommand to CliMetadataProvider (AC: #2)
  - [x] 6.1: Update `GenerateCommand.Execute()` to create `CliMetadataProvider` with solution path
  - [x] 6.2: Change return type to `int` for exit code support
  - [x] 6.3: Use `this.SetHandler()` with handler that returns exit code
  - [x] 6.4: Output success message using `ConsoleOutput.Success()` after successful load

- [x] Task 7: Add Unit Tests (AC: #1, #2, #3, #4)
  - [x] 7.1: Create `src/Tests/CLI/CliMetadataProviderTests.cs`
  - [x] 7.2: Test valid solution path loads workspace successfully
  - [x] 7.3: Test invalid path returns appropriate error
  - [x] 7.4: Test non-existent file returns file not found error
  - [x] 7.5: Use xUnit `[Fact]` and `[Theory]` with `Should` assertions

## Dev Notes

### Critical Constraints (MUST FOLLOW)

- **.NET Framework 4.7.2 MANDATORY** - No C# 8+ features
- **NO async Main** - Use `.GetAwaiter().GetResult()` for sync-over-async patterns
- **NO `ThreadHelper`** - This is VS-specific, cannot use in CLI
- **Use `string.Empty`** instead of `""` for empty strings
- **Lazy initialization pattern:** `_field ?? (_field = LoadField())`
- **NEVER use `Console.WriteLine`** - Use `ConsoleOutput` static class only

### Why CliFileMetadata is Needed

The existing `RoslynFileMetadata` class uses `ThreadHelper.JoinableTaskFactory.Run()` (line 47-51 in `src/Roslyn/RoslynFileMetadata.cs`) which requires Visual Studio's threading infrastructure. The CLI must create `CliFileMetadata` that:
1. Uses `.GetAwaiter().GetResult()` instead of `ThreadHelper`
2. Copies the same metadata extraction logic (Classes, Interfaces, Enums, Records, Delegates)
3. Implements identical `GetNamespaceChildNodes<T>()` logic

### Buildalyzer Integration Pattern

```csharp
// CliMetadataProvider.cs - Key pattern
using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;

public class CliMetadataProvider : IMetadataProvider
{
    private readonly string _solutionPath;
    private AdhocWorkspace _workspace;

    public CliMetadataProvider(string solutionPath)
    {
        _solutionPath = solutionPath;
    }

    private AdhocWorkspace Workspace
    {
        get { return _workspace ?? (_workspace = LoadWorkspace()); }
    }

    private AdhocWorkspace LoadWorkspace()
    {
        var manager = new AnalyzerManager(_solutionPath);
        return manager.GetWorkspace(); // From Buildalyzer.Workspaces
    }

    public IFileMetadata GetFile(string path, Settings settings, Action<string[]> requestRender)
    {
        var documentId = Workspace.CurrentSolution.GetDocumentIdsWithFilePath(path).FirstOrDefault();
        if (documentId != null)
        {
            var document = Workspace.CurrentSolution.GetDocument(documentId);
            return new CliFileMetadata(document, settings, requestRender);
        }
        return null;
    }
}
```

### CliFileMetadata Pattern (Replace ThreadHelper)

```csharp
// CliFileMetadata.cs - Replace VS-specific RoslynFileMetadata
private void LoadDocument(Document document)
{
    _document = document;
    // Use sync-over-async pattern instead of ThreadHelper
    _semanticModel = document.GetSemanticModelAsync().GetAwaiter().GetResult();
    _root = _semanticModel.SyntaxTree.GetRootAsync().GetAwaiter().GetResult();
}
```

### Exit Code Implementation Pattern

```csharp
// GenerateCommand.cs - Exit code support
public GenerateCommand() : base("generate", "Generate TypeScript files from C# code using templates")
{
    var solutionOption = new Option<string>(
        aliases: new[] { "--solution", "-s" },
        description: "Path to the Visual Studio solution file (.sln)");

    AddOption(solutionOption);

    this.SetHandler((InvocationContext context) =>
    {
        var solution = context.ParseResult.GetValueForOption(solutionOption);
        context.ExitCode = Execute(solution);
    });
}

private static int Execute(string solution)
{
    // Validation - exit code 2 for invalid args
    if (string.IsNullOrEmpty(solution))
    {
        ConsoleOutput.Error("Error: --solution argument is required");
        return 2;
    }

    if (!File.Exists(solution))
    {
        ConsoleOutput.Error(string.Concat(solution, ": Error: File not found"));
        return 2;
    }

    if (!solution.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
    {
        ConsoleOutput.Error(string.Concat(solution, ": Error: Not a solution file (.sln)"));
        return 2;
    }

    try
    {
        var provider = new CliMetadataProvider(solution);
        // Trigger workspace loading to validate solution
        var workspace = provider.TestWorkspace; // Or call any method that triggers load
        ConsoleOutput.Success(string.Concat("Loaded solution: ", solution));
        return 0;
    }
    catch (Exception ex)
    {
        ConsoleOutput.Error(string.Concat(solution, ": Error: ", ex.Message));
        return 1;
    }
}
```

### Error Message Format

Follow compiler-style format for all errors:
```
FilePath: Error: Description of the problem
```

Examples:
```
./MyProject.sln: Error: File not found
./MyProject.sln: Error: Failed to parse solution file
./MyProject.sln: Error: Project 'WebApi.csproj' failed to build
```

### Required Project References (Add to Typewriter.CLI.csproj)

```xml
<ProjectReference Include="..\..\Buildalyzer\src\Buildalyzer\Buildalyzer.csproj">
  <Project>{BUILDALYZER-GUID}</Project>
  <Name>Buildalyzer</Name>
</ProjectReference>
<ProjectReference Include="..\..\Buildalyzer\src\Buildalyzer.Workspaces\Buildalyzer.Workspaces.csproj">
  <Project>{BUILDALYZER-WORKSPACES-GUID}</Project>
  <Name>Buildalyzer.Workspaces</Name>
</ProjectReference>
```

Note: Get actual GUIDs from Buildalyzer project files.

### Project Structure After This Story

```
src/CLI/
├── Typewriter.CLI.csproj     (modified - add Buildalyzer refs)
├── Program.cs                 (unchanged)
├── Commands/
│   └── GenerateCommand.cs     (modified - add exit code support)
├── Infrastructure/
│   ├── ConsoleOutput.cs       (unchanged)
│   ├── CliMetadataProvider.cs (NEW)
│   └── CliFileMetadata.cs     (NEW)
└── Properties/
    └── AssemblyInfo.cs        (unchanged)

src/Tests/CLI/
├── ConsoleOutputTests.cs      (unchanged)
└── CliMetadataProviderTests.cs (NEW)
```

### Anti-Patterns to AVOID

- DO NOT use `ThreadHelper` or any VS-specific APIs
- DO NOT use `Console.WriteLine` directly - use `ConsoleOutput`
- DO NOT use async Main - use `.GetAwaiter().GetResult()`
- DO NOT create a new metadata project - use CLI/Infrastructure folder
- DO NOT modify existing `RoslynFileMetadata` - create new `CliFileMetadata`
- DO NOT skip exit codes - they are required for scripting integration
- DO NOT use C# 8+ features (nullable references, switch expressions)
- DO NOT add docstrings or comments unless essential

### File Dependencies

| File | Purpose | Depends On |
|------|---------|------------|
| `CliMetadataProvider.cs` | Workspace management | Buildalyzer, Buildalyzer.Workspaces |
| `CliFileMetadata.cs` | File metadata extraction | Microsoft.CodeAnalysis |
| `GenerateCommand.cs` | CLI integration | CliMetadataProvider, ConsoleOutput |

### Testing Strategy

Use test fixtures from `src/Tests/CodeModel/Support/` for sample C# files and solutions if available. Otherwise create minimal test fixtures:

```csharp
// CliMetadataProviderTests.cs
public class CliMetadataProviderTests
{
    [Fact]
    public void Constructor_WithValidSolution_ShouldNotThrow()
    {
        // Arrange - use a test solution if available
        var solutionPath = GetTestSolutionPath();

        // Act
        var provider = new CliMetadataProvider(solutionPath);

        // Assert - no exception thrown
    }

    [Fact]
    public void GetFile_WithValidCsFile_ShouldReturnMetadata()
    {
        // Arrange
        var solutionPath = GetTestSolutionPath();
        var provider = new CliMetadataProvider(solutionPath);
        var csFilePath = GetTestCsFilePath();

        // Act
        var result = provider.GetFile(csFilePath, new Settings(), null);

        // Assert
        result.ShouldNotBeNull();
    }
}
```

### References

- [Source: _bmad-output/planning-artifacts/architecture.md#Workspace-Provider]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns-Consistency-Rules]
- [Source: _bmad-output/project-context.md#Critical-Implementation-Rules]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.2]
- [Source: src/Roslyn/RoslynMetadataProvider.cs - IMetadataProvider pattern]
- [Source: src/Roslyn/RoslynFileMetadata.cs - File metadata extraction pattern]
- [Source: Buildalyzer/src/Buildalyzer.Workspaces/AnalyzerManagerExtensions.cs - GetWorkspace pattern]
- [Source: src/CLI/Infrastructure/ConsoleOutput.cs - Console output pattern]

### Previous Story Learnings (from 1-1)

- System.CommandLine 2.0.0-beta4.22272.1 is already configured
- `this.SetHandler()` pattern established for command handlers
- ConsoleOutput correctly routes errors to stderr
- Traditional .csproj format works correctly with solution
- SharedAssemblyInfo.cs provides version info

## Dev Agent Record

### Agent Model Used

Claude Opus 4.5 (claude-opus-4-5-20251101)

### Debug Log References

None

### Completion Notes List

- Added Buildalyzer and Buildalyzer.Workspaces project references to CLI
- Created CliMetadataProvider implementing IMetadataProvider with lazy workspace loading
- Created CliFileMetadata using sync-over-async pattern (.GetAwaiter().GetResult())
- Added InternalsVisibleTo in Roslyn assembly for CLI to access internal metadata methods
- Implemented full validation in GenerateCommand with exit codes (2 for invalid args, 1 for failures)
- Added binding redirects in App.config for assembly version conflicts
- All 11 CLI tests pass (ConsoleOutput, CliMetadataProvider, GenerateCommand)
- CLI successfully loads solution: `typewriter generate --solution Typewriter.sln` → exit 0

### File List

**New Files:**
- `src/CLI/Infrastructure/CliMetadataProvider.cs`
- `src/CLI/Infrastructure/CliFileMetadata.cs`
- `src/Tests/CLI/CliMetadataProviderTests.cs`
- `src/Tests/CLI/GenerateCommandTests.cs`

**Modified Files:**
- `src/CLI/Typewriter.CLI.csproj` - Added Buildalyzer project references
- `src/CLI/Commands/GenerateCommand.cs` - Added validation and exit code support
- `src/CLI/App.config` - Added binding redirects for assembly version conflicts
- `src/Roslyn/Properties/AssemblyInfo.cs` - Added InternalsVisibleTo for CLI and Tests
