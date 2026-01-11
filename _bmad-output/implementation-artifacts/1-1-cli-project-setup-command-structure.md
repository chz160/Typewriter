# Story 1.1: CLI Project Setup & Command Structure

Status: done

## Story

As a **developer**,
I want **a Typewriter CLI executable with help and version commands**,
So that **I can discover available options and verify the installed version**.

## Acceptance Criteria

1. **Given** the Typewriter.CLI project does not exist **When** the project is created following architecture specifications **Then** `src/CLI/Typewriter.CLI.csproj` exists targeting .NET Framework 4.7.2 **And** the project references `Typewriter.CodeModel`, `Typewriter.Metadata`, and `Typewriter.Metadata.Roslyn` **And** the project references `System.CommandLine` for argument parsing **And** the project builds successfully as part of `Typewriter.sln`

2. **Given** the CLI executable exists **When** a user runs `typewriter --help` **Then** help text displays showing available commands and options **And** the `generate` command is listed with its description

3. **Given** the CLI executable exists **When** a user runs `typewriter --version` **Then** the version number is displayed (e.g., "Typewriter CLI v1.0.0")

4. **Given** the CLI executable exists **When** a user runs `typewriter generate --help` **Then** help text displays showing the `--solution` option and its description

**FRs Covered:** FR23, FR24

## Tasks / Subtasks

- [x] Task 1: Create CLI Project Structure (AC: #1)
  - [x] 1.1: Create `src/CLI/Typewriter.CLI.csproj` targeting .NET Framework 4.7.2
  - [x] 1.2: Add project references to `Typewriter.CodeModel`, `Typewriter.Metadata`, `Typewriter.Metadata.Roslyn`
  - [x] 1.3: Add NuGet reference to `System.CommandLine` (latest stable compatible with .NET Framework 4.7.2)
  - [x] 1.4: Add project to `Typewriter.sln`
  - [x] 1.5: Create folder structure: `Commands/`, `Infrastructure/`, `Configuration/`, `Properties/`

- [x] Task 2: Implement Program Entry Point (AC: #2, #3)
  - [x] 2.1: Create `Program.cs` with Main entry point (NO async Main - use `.GetAwaiter().GetResult()`)
  - [x] 2.2: Configure System.CommandLine root command with description
  - [x] 2.3: Add `--version` option using System.CommandLine built-in support
  - [x] 2.4: Wire up command execution

- [x] Task 3: Implement Generate Command Stub (AC: #2, #4)
  - [x] 3.1: Create `Commands/GenerateCommand.cs` with `*Command` suffix naming
  - [x] 3.2: Add `--solution` option with description
  - [x] 3.3: Wire command to root command in Program.cs
  - [x] 3.4: Add placeholder handler that returns success

- [x] Task 4: Implement ConsoleOutput Infrastructure (AC: #2, #3, #4)
  - [x] 4.1: Create `Infrastructure/ConsoleOutput.cs` static class
  - [x] 4.2: Implement Success (green), Error (red), Warning (yellow), Info (default) methods
  - [x] 4.3: Use ANSI escape codes for colors (zero external dependencies)
  - [x] 4.4: Ensure errors go to stderr, normal output to stdout

- [x] Task 5: Verify Build Integration (AC: #1)
  - [x] 5.1: Build entire solution with `msbuild Typewriter.sln /p:Configuration=Release`
  - [x] 5.2: Verify Typewriter.CLI.exe is produced in output directory
  - [x] 5.3: Test `--help`, `--version`, and `generate --help` commands

- [x] Task 6: Add Unit Tests (AC: #1, #2, #3, #4)
  - [x] 6.1: Create `src/Tests/CLI/` subfolder (DO NOT create separate test project)
  - [x] 6.2: Add `ConsoleOutputTests.cs` with xUnit tests
  - [x] 6.3: Use `Should` fluent assertions pattern: `result.ShouldBe(expected)`

## Dev Notes

### Critical Constraints

- **.NET Framework 4.7.2 MANDATORY** - No C# 8+ features (no nullable references, no switch expressions, no default interface implementations)
- **NO async Main** - Use `.GetAwaiter().GetResult()` for sync-over-async in entry point
- **Use `string.Empty`** instead of `""` for empty strings
- **Lazy initialization pattern:** `_field ?? (_field = ...)`

### Required Dependencies

| Package | Version | Notes |
|---------|---------|-------|
| System.CommandLine | Latest stable | Official Microsoft CLI parser |
| Microsoft.CodeAnalysis | 4.14.0 | MUST match VS extension exactly |

### Console Output Implementation

```csharp
// ConsoleOutput.cs - Use ANSI escape codes
public static class ConsoleOutput
{
    private const string Green = "\u001b[32m";
    private const string Red = "\u001b[31m";
    private const string Yellow = "\u001b[33m";
    private const string Reset = "\u001b[0m";

    public static void Success(string message) => Console.WriteLine($"{Green}{message}{Reset}");
    public static void Error(string message) => Console.Error.WriteLine($"{Red}{message}{Reset}");
    public static void Warning(string message) => Console.WriteLine($"{Yellow}{message}{Reset}");
    public static void Info(string message) => Console.WriteLine(message);
}
```

### Program.cs Pattern

```csharp
// NO async Main - .NET Framework 4.7.2 requirement
public static int Main(string[] args)
{
    var rootCommand = new RootCommand("Typewriter CLI - Generate TypeScript from C# templates");
    rootCommand.AddCommand(new GenerateCommand());
    return rootCommand.Invoke(args);
}
```

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Generation failure |
| 2 | Invalid arguments |

### Project Structure Notes

**Target directory structure:**
```
src/CLI/
├── Typewriter.CLI.csproj
├── Program.cs
├── Commands/
│   └── GenerateCommand.cs
├── Infrastructure/
│   └── ConsoleOutput.cs
└── Properties/
    └── AssemblyInfo.cs

src/Tests/CLI/
└── ConsoleOutputTests.cs
```

### Alignment with Existing Patterns

- Follow `*Impl` suffix pattern for any implementation wrappers (future stories)
- Follow `*Command` suffix for command classes
- Use `ConsoleOutput` static class for ALL user-facing messages
- NEVER use `Console.WriteLine` directly in command handlers
- Tests use xUnit `[Fact]` and `[Theory]`, `Should` assertions, `NSubstitute` mocking

### Anti-Patterns to AVOID

- DO NOT create separate test project for CLI tests (use `src/Tests/CLI/`)
- DO NOT use `Console.WriteLine` directly (use `ConsoleOutput`)
- DO NOT use async Main (use sync with `.GetAwaiter().GetResult()`)
- DO NOT use C# 8+ features (nullable reference types, switch expressions, etc.)
- DO NOT add comments, docstrings, or type annotations to code you don't write

### References

- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure-Boundaries]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns-Consistency-Rules]
- [Source: _bmad-output/project-context.md#Critical-Implementation-Rules]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.1]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.5 (claude-opus-4-5-20251101)

### Debug Log References

- Build succeeded with 0 errors on MSBuild Release configuration
- CLI --help, --version, generate --help commands all verified working
- All 5 ConsoleOutput unit tests passed

### Completion Notes List

- Created Typewriter.CLI project targeting .NET Framework 4.7.2 with traditional csproj format
- Added project references to CodeModel, Metadata, and Metadata.Roslyn assemblies
- Added System.CommandLine 2.0.0-beta4.22272.1 NuGet package for CLI argument parsing
- Implemented Program.cs with synchronous Main entry point (no async Main per .NET Framework 4.7.2 requirement)
- Implemented GenerateCommand with --solution option as a stub for future implementation
- Implemented ConsoleOutput static class with ANSI color codes for Success/Error/Warning/Info methods
- Error output correctly routes to stderr while normal output goes to stdout
- Added CLI project to Typewriter.sln with proper build configurations
- Created 5 unit tests for ConsoleOutput class validating color codes and stream routing
- Version output displays assembly version from SharedAssemblyInfo.cs (2.12.1.*)

### File List

**New Files:**
- src/CLI/Typewriter.CLI.csproj
- src/CLI/Program.cs
- src/CLI/App.config
- src/CLI/Commands/GenerateCommand.cs
- src/CLI/Infrastructure/ConsoleOutput.cs
- src/CLI/Properties/AssemblyInfo.cs
- src/Tests/CLI/ConsoleOutputTests.cs

**Modified Files:**
- Typewriter.sln (added CLI project entry and build configurations)
- src/Tests/Typewriter.Tests.csproj (added CLI project reference)

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2026-01-10 | Initial implementation of CLI project structure and command framework | Claude Opus 4.5 |
