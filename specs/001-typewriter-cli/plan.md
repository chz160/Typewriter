# Implementation Plan: Typewriter CLI Extension

**Branch**: `001-typewriter-cli` | **Date**: 2026-01-11 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-typewriter-cli/spec.md`

## Summary

Typewriter CLI extends the existing Typewriter Visual Studio extension to provide command-line TypeScript generation from C# code. The CLI enables developers using VS Code, JetBrains Rider, or any text editor to leverage Typewriter's template-based code generation without requiring Visual Studio.

**Technical Approach**: Multi-target architecture with shared .NET Standard 2.0 core library (`Typewriter.Core`) enabling maximum code reuse between the existing VS extension (.NET Framework 4.7.2) and the new CLI (.NET 8). The CLI uses Buildalyzer + AdhocWorkspace for standalone Roslyn analysis, System.CommandLine for argument parsing, and reuses the existing template engine (~90% code reuse).

## Technical Context

**Language/Version**: C# 12 / .NET 8 (CLI), .NET Standard 2.0 (shared core), .NET Framework 4.7.2 (VS extension)
**Primary Dependencies**:
- System.CommandLine (CLI argument parsing)
- Microsoft.CodeAnalysis 4.14.0 (Roslyn - same version as VS extension)
- Buildalyzer (project/solution loading outside VS)
- Newtonsoft.Json (configuration file parsing)
**Storage**: N/A (file-based generation, no persistence layer)
**Testing**: xUnit with Should fluent assertions, NSubstitute for mocking
**Target Platform**: Windows 10/11 with .NET 8 runtime
**Project Type**: Multi-project solution (brownfield extension)
**Performance Goals**: <3s cold start, <500ms per C# file, <30s solution load for 100-project solutions
**Constraints**: <2GB memory, byte-identical output parity with VS extension, zero VS dependencies in CLI path
**Scale/Scope**: Typical solutions with 10-100 projects, hundreds of C# files, dozens of .tst templates

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Upstream Style Conformance
| Requirement | Status | Notes |
|-------------|--------|-------|
| PascalCase naming | PASS | All new classes follow existing conventions |
| Interface `I` prefix | PASS | Core interfaces extracted maintain naming |
| `*Impl` suffix for implementations | PASS | CLI implementations: `CliClassImpl`, `CliPropertyImpl`, etc. |
| `Roslyn*` prefix for Roslyn implementations | N/A | CLI uses `Cli*` prefix for CLI-specific code |
| Private fields `_camelCase` | PASS | Following existing pattern |
| XML documentation on public API | PASS | Required for all new public members |

### II. Test Coverage Requirements
| Requirement | Status | Notes |
|-------------|--------|-------|
| xUnit with `[Fact]` attributes | PASS | All new tests follow this |
| Should fluent assertions | PASS | Using existing assertion library |
| NSubstitute for mocking | PASS | CLI tests will mock metadata providers |
| CLI tests in `src/Tests/CLI/` | PASS | Per architecture document |
| No VS dependencies in CLI tests | PASS | CLI tests are VS-independent |

### III. Architectural Consistency
| Requirement | Status | Notes |
|-------------|--------|-------|
| Abstract models in `src/CodeModel/` | PASS | Unchanged - already netstandard2.0 |
| Metadata interfaces in `src/Metadata/` | PASS | Unchanged - already netstandard2.0 |
| `FromMetadata()` static factory pattern | PASS | CLI implementations follow this |
| Lazy initialization pattern | PASS | For expensive Roslyn operations |
| Settings threaded through constructors | PASS | CLI passes settings to implementations |

### IV. Visual Studio Extension Compatibility
| Requirement | Status | Notes |
|-------------|--------|-------|
| VS extension remains .NET 4.7.2 | PASS | No changes to VS extension target |
| No VS SDK dependencies in shared code | PASS | Core library is VS-independent |
| Existing VS tests continue passing | PASS | No modifications to VS extension code |

### V. Performance and Resource Efficiency
| Requirement | Status | Notes |
|-------------|--------|-------|
| Lazy loading for Roslyn operations | PASS | CliMetadataProvider defers analysis |
| Caching for metadata lookups | PASS | Per-generation cycle caching |
| Memory bounded <2GB | PASS | Architectural constraint |
| Cancellation token support | PASS | CLI supports Ctrl+C cancellation |

**Constitution Gate Status**: PASS - All applicable requirements satisfied

## Project Structure

### Documentation (this feature)

```text
specs/001-typewriter-cli/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (CLI interface contracts)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
# Existing Projects (unchanged or minimal changes)
src/
├── CodeModel/                      # Typewriter.CodeModel.dll (netstandard2.0) - DONE
├── Metadata/                       # Typewriter.Metadata.dll (netstandard2.0) - DONE
├── Roslyn/                         # Typewriter.Metadata.Roslyn.dll (net472) - unchanged
├── Typewriter/                     # VS extension (net472) - minimal changes
│   └── Generation/                 # Template engine to extract to Core
└── Tests/                          # Existing tests
    ├── Core/                       # NEW - Core library tests
    └── CLI/                        # NEW - CLI tests

# New Projects
src/
├── Core/                           # NEW - Typewriter.Core (netstandard2.0)
│   ├── Typewriter.Core.csproj
│   ├── Generation/                 # Extracted template engine
│   │   ├── Parser.cs
│   │   ├── Compiler.cs
│   │   ├── TemplateCodeParser.cs
│   │   ├── ItemFilter.cs
│   │   └── Template.cs
│   └── Abstractions/               # Provider interfaces
│       ├── IGenerationContext.cs
│       └── IErrorReporter.cs
│
└── CLI/                            # NEW - Typewriter.CLI (net8.0)
    ├── Typewriter.CLI.csproj
    ├── Program.cs                  # Entry point
    ├── Commands/
    │   └── GenerateCommand.cs
    ├── Infrastructure/
    │   ├── CliMetadataProvider.cs
    │   ├── CliRoslynWorkspace.cs
    │   ├── ConsoleOutput.cs
    │   ├── TemplateFinder.cs
    │   └── PathResolver.cs
    ├── Configuration/
    │   └── CliSettings.cs
    └── CodeModel/
        └── Implementation/         # CLI-specific *Impl classes
            ├── CliClassImpl.cs
            ├── CliPropertyImpl.cs
            └── ...
```

**Structure Decision**: Multi-project brownfield extension following existing solution patterns. Two new projects (`Typewriter.Core` and `Typewriter.CLI`) integrate into existing `Typewriter.sln`. Shared code extracted to Core enables maximum reuse while CLI-specific implementations remain isolated.

## Complexity Tracking

> No constitution violations requiring justification. Architecture follows all established patterns.

| Aspect | Approach | Justification |
|--------|----------|---------------|
| Two new projects | Core + CLI separation | Required for multi-target (.NET Standard 2.0 bridges .NET 4.7.2 and .NET 8) |
| CLI-specific *Impl classes | Mirrors VS pattern | Maintains architectural consistency, allows Buildalyzer integration |

## Implementation Phases

### Phase 1: Typewriter.Core (netstandard2.0) - Shared Library

**Objective**: Extract VS-independent generation logic to shared library

1. Create `src/Core/Typewriter.Core.csproj` targeting netstandard2.0
2. Extract template engine components:
   - `Generation/Parser.cs` - Template parsing
   - `Generation/Compiler.cs` - Template compilation
   - `Generation/TemplateCodeParser.cs` - C# code block parsing
   - `Generation/ItemFilter.cs` - Template item filtering
   - `Generation/Template.cs` - Core template logic
3. Create abstraction interfaces:
   - `IGenerationContext` - Generation environment abstraction
   - `IErrorReporter` - Error/warning reporting abstraction
4. Update VS extension to reference `Typewriter.Core`
5. Verify all existing functionality unchanged

**Dependencies**: None (foundation phase)
**Tests**: `src/Tests/Core/` - Parser, Compiler, Template tests

### Phase 2: Typewriter.CLI (net8.0) - CLI Implementation

**Objective**: Implement command-line interface for TypeScript generation

1. Create `src/CLI/Typewriter.CLI.csproj` targeting net8.0
2. Implement infrastructure:
   - `ConsoleOutput.cs` - ANSI-colored output utilities
   - `CliRoslynWorkspace.cs` - Buildalyzer + AdhocWorkspace integration
   - `CliMetadataProvider.cs` - IMetadataProvider implementation
   - `TemplateFinder.cs` - .tst file discovery
   - `PathResolver.cs` - Relative path resolution
3. Implement CLI-specific code model wrappers (`Cli*Impl.cs` classes)
4. Implement `GenerateCommand.cs` with System.CommandLine
5. Wire up `Program.cs` entry point

**Dependencies**: Typewriter.Core
**Tests**: `src/Tests/CLI/` - Command, Provider, Output tests

### Phase 3: Growth Features (Post-MVP)

**Objective**: Add configuration file support and advanced output modes

1. Implement `CliSettings.cs` for config file parsing
2. Add config file discovery (`.typewriterrc`, `typewriter.json`)
3. Implement `--verbose`, `--quiet`, `--dry-run` flags
4. Add JSON output format (`--json`)

**Dependencies**: Phases 1-2 complete
**Tests**: Configuration parsing, output mode tests

## Risk Mitigation

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Workspace provider differs from VS | Medium | Comprehensive test suite comparing CLI vs VS output |
| Template compilation edge cases | Low | Reusing existing proven compiler code |
| Buildalyzer compatibility issues | Low | Already in solution as submodule, proven technology |
| Performance regression | Low | .NET 8 has faster startup than .NET Framework |

## Success Criteria Verification

| Criteria | Verification Method |
|----------|---------------------|
| SC-001: Generate without VS | Run `typewriter generate --solution <path>` from terminal |
| SC-002: Single command execution | Verify one command produces all TypeScript files |
| SC-003: Byte-identical output | Diff CLI output against VS extension output |
| SC-004: Unchanged templates | Use existing .tst files without modification |
| SC-005: Error diagnostics | Introduce deliberate errors, verify file:line:column output |
| SC-006: <3s cold start | Measure startup time on typical solutions |
| SC-007: <500ms per file | Profile per-file generation time |
| SC-010: Deterministic output | Run multiple times, verify identical output |
