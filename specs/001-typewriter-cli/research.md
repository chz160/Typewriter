# Research Document: Typewriter CLI Extension

**Feature Branch**: `001-typewriter-cli`
**Date**: 2026-01-11
**Status**: Complete

## Overview

This document consolidates research findings for the Typewriter CLI implementation. All critical decisions were made during the architecture phase based on comprehensive analysis of the existing codebase and PRD requirements.

## Technology Decisions

### 1. CLI Argument Parsing Library

**Decision**: System.CommandLine

**Rationale**:
- Official Microsoft library, actively maintained
- Better acceptance for upstream PR contributions
- Built-in support for --help, --version, subcommands
- Strong typing for command options and arguments
- Async command handlers align with Roslyn's async APIs

**Alternatives Considered**:
| Library | Pros | Cons | Verdict |
|---------|------|------|---------|
| System.CommandLine | Official MS, rich features | Newer API | **Selected** |
| CommandLineParser | Mature, widely used | Not official MS, older patterns | Rejected |
| Spectre.Console.Cli | Beautiful output | Additional dependency, less mainstream | Rejected |

### 2. Standalone Workspace Provider

**Decision**: Buildalyzer + AdhocWorkspace

**Rationale**:
- Buildalyzer already exists in solution as git submodule
- Purpose-built for analyzing .NET projects outside Visual Studio
- Works with MSBuild project files directly
- AdhocWorkspace provides Roslyn workspace without VS dependencies
- Proven compatibility with same Roslyn version (4.14.0)

**Alternatives Considered**:
| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| Buildalyzer + AdhocWorkspace | Already in solution, proven | Learning curve | **Selected** |
| MSBuildWorkspace | Official Roslyn workspace | Complex setup, VS dependencies | Rejected |
| Direct Roslyn compilation | Full control | Significant implementation effort | Rejected |

### 3. Configuration File Format

**Decision**: JSON format with Newtonsoft.Json

**Rationale**:
- JSON is universal, human-readable, well-understood
- Newtonsoft.Json likely already in solution dependency tree
- Battle-tested library with excellent .NET ecosystem support
- Supports comments via JsonReaderSettings (if needed)
- Familiar pattern for developer tooling (.eslintrc, tsconfig.json)

**Alternatives Considered**:
| Format | Pros | Cons | Verdict |
|--------|------|------|---------|
| JSON (Newtonsoft.Json) | Universal, familiar, proven | Verbose for complex configs | **Selected** |
| YAML | More readable | Requires additional dependency | Rejected |
| TOML | Clean syntax | Less .NET ecosystem support | Rejected |

### 4. Console Output Strategy

**Decision**: Plain text with ANSI colors

**Rationale**:
- Zero additional dependencies
- Native terminal support on Windows 10+
- Familiar developer experience (matches dotnet CLI, npm, etc.)
- Easily parsed by CI/CD systems
- Compiler-style error format (File:Line:Column: Message) is IDE-parseable

**Output Format Examples**:

**Success**:
```
Typewriter CLI v1.0.0
Processing solution: ./MyProject.sln
Found 3 templates

Generated 12 TypeScript files in 1.2s
```

**Error**:
```
Error: Template compilation failed
  CustomerModel.tst:15:8: Cannot resolve type 'OrderStatus'
```

**Color Scheme**:
| Level | Color | Usage |
|-------|-------|-------|
| Success | Green | Completion messages, file counts |
| Error | Red | Fatal errors, validation failures |
| Warning | Yellow | Non-fatal issues, skipped files |
| Info | Default | Normal output, file lists |

### 5. Multi-Target Framework Strategy

**Decision**: Three-tier architecture with .NET Standard 2.0 bridge

**Framework Matrix**:
| Project | Target | Rationale |
|---------|--------|-----------|
| Typewriter.Core (NEW) | netstandard2.0 | Bridges legacy and modern runtimes |
| Typewriter.CLI (NEW) | net8.0 | Modern runtime, cross-platform ready |
| Typewriter (VS Extension) | net472 | VS extension requirement (unchanged) |
| Typewriter.CodeModel | netstandard2.0 | Already migrated, shared |
| Typewriter.Metadata | netstandard2.0 | Already migrated, shared |
| Typewriter.Metadata.Roslyn | net472 | VS-specific, cannot migrate |

**Rationale**:
- netstandard2.0 is consumable by both net472 and net8.0
- Enables 90%+ code reuse via shared Typewriter.Core library
- Future-proofs for eventual full .NET migration
- No breaking changes to existing VS extension

### 6. Exit Code Strategy

**Decision**: Per PRD specification (0/1/2)

| Exit Code | Meaning | Trigger |
|-----------|---------|---------|
| 0 | Success | All files generated successfully |
| 1 | Generation failure | Template errors, missing files, compilation errors |
| 2 | Invalid arguments | Bad CLI args, missing required options, config errors |

**Rationale**:
- Standard CLI convention
- Enables CI/CD pipeline integration
- Distinguishes user errors (2) from processing errors (1)
- Scriptable with standard shell constructs

### 7. Stream Separation

**Decision**: stdout for success output, stderr for errors

**Rationale**:
- Standard POSIX convention
- Enables output redirection and piping
- CI/CD systems can capture streams separately
- Allows `typewriter generate 2>errors.log` patterns

## Code Reuse Analysis

### Components to Extract to Typewriter.Core

| Component | Source | Lines (est.) | Dependencies |
|-----------|--------|--------------|--------------|
| Parser.cs | Typewriter/Generation/ | ~200 | None (VS-independent) |
| Compiler.cs | Typewriter/Generation/ | ~150 | None (VS-independent) |
| TemplateCodeParser.cs | Typewriter/Generation/ | ~100 | None (VS-independent) |
| ItemFilter.cs | Typewriter/Generation/ | ~50 | None (VS-independent) |
| Template.cs | Typewriter/Generation/ | ~300 | May need abstraction |

**Total Extracted**: ~800 lines moved to shared core
**Estimated New CLI Code**: ~920 lines

### VS Dependencies Requiring Abstraction

The following VS-specific patterns need abstraction in Core:

1. **ThreadHelper.ThrowIfNotOnUIThread()** - Remove/conditionally compile
2. **ServiceProvider** - Replace with dependency injection
3. **VisualStudioWorkspace** - Already abstracted via IMetadataProvider
4. **DTE (EnvDTE)** - Not needed in Core, stays in VS extension

## Performance Considerations

### Cold Start Optimization

Target: <3 seconds cold start

Strategies:
- .NET 8 has faster JIT than .NET Framework
- Lazy initialization for Roslyn workspace
- Defer project loading until first generation
- Consider ReadyToRun compilation for published CLI

### Per-File Generation

Target: <500ms per C# file

Strategies:
- Reuse workspace across multiple files
- Cache compiled templates
- Parallel template processing where possible
- Minimize file I/O operations

### Memory Efficiency

Target: <2GB for typical solutions

Strategies:
- Lazy loading for Roslyn symbol resolution
- Release documents after generation
- Use streaming for large template outputs
- Per-generation cycle caching (not permanent)

## Integration Points

### Buildalyzer Integration

```
CliMetadataProvider
    └── Buildalyzer.AnalyzerManager
            └── AnalyzerResult (per project)
                    └── AdhocWorkspace
                            └── Project/Documents
```

**Key API Surfaces**:
- `AnalyzerManager.GetProject(projectPath)` - Load project
- `AnalyzerResult.GetWorkspace()` - Get Roslyn workspace
- `Workspace.CurrentSolution.Projects` - Enumerate projects

### Template Engine Integration

```
GenerateCommand
    └── CliMetadataProvider
            └── IFileMetadata (per C# file)
                    └── Template.Render(metadata)
                            └── TypeScript output
```

**Key API Surfaces**:
- `IMetadataProvider.GetFile(path)` - Get file metadata
- `Template.Parse(templateContent)` - Parse .tst template
- `Template.Render(fileMetadata)` - Generate TypeScript

## Testing Strategy

### Unit Tests (src/Tests/CLI/)

| Test Class | Coverage |
|------------|----------|
| GenerateCommandTests | Command parsing, option handling |
| CliMetadataProviderTests | Workspace loading, file metadata |
| TemplateFinderTests | .tst file discovery patterns |
| ConsoleOutputTests | Color output, message formatting |
| PathResolverTests | Relative path resolution |

### Integration Tests

| Test Scenario | Verification |
|---------------|--------------|
| End-to-end generation | CLI produces TypeScript files |
| Output parity | CLI output matches VS extension output |
| Error handling | Template errors produce correct diagnostics |
| Exit codes | Correct codes for success/failure scenarios |

### Existing Test Preservation

The existing Typewriter.Tests project requires VS SDK infrastructure (MefHostingFixture, ThreadHelper) and cannot run outside Visual Studio. New CLI tests are designed to be VS-independent and run via standard `dotnet test` command.

## Resolved Questions

All technical questions identified during specification have been resolved:

| Question | Resolution |
|----------|------------|
| CLI argument library | System.CommandLine (official MS) |
| Workspace provider | Buildalyzer + AdhocWorkspace (already in solution) |
| Config format | JSON via Newtonsoft.Json |
| Output format | Plain text with ANSI colors |
| Error format | Compiler-style (File:Line:Column: Message) |
| Framework target | net8.0 for CLI, netstandard2.0 for shared Core |
| Exit codes | 0=success, 1=generation failure, 2=invalid args |

## References

- PRD: `_bmad-output/planning-artifacts/prd.md`
- Architecture: `_bmad-output/planning-artifacts/architecture.md`
- Buildalyzer: `Buildalyzer/` (git submodule)
- System.CommandLine: https://github.com/dotnet/command-line-api
- Roslyn: Microsoft.CodeAnalysis 4.14.0
