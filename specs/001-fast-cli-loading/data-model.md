# Data Model: Fast CLI Project Loading

**Feature**: 001-fast-cli-loading
**Date**: 2026-01-23
**Status**: Complete

## Overview

This document defines the internal data structures for the fast CLI project loading feature. These are implementation entities, not user-facing APIs.

## Entities

### ProjectInfo

Represents a parsed .csproj file with its metadata and source files.

| Field | Type | Description |
|-------|------|-------------|
| ProjectPath | string | Absolute path to .csproj file |
| ProjectDirectory | string | Directory containing .csproj |
| ProjectName | string | Name derived from file name |
| IsSdkStyle | bool | True if SDK-style project |
| TargetFramework | string? | Target framework moniker (e.g., "net8.0") |
| SourceFiles | IReadOnlyList\<string\> | Absolute paths to discovered .cs files |
| ProjectReferences | IReadOnlyList\<string\> | Relative paths to referenced .csproj files |
| AssemblyName | string? | Output assembly name |
| RootNamespace | string? | Default namespace |

**Validation Rules**:
- ProjectPath must exist and be a valid .csproj file
- SourceFiles must all exist and have .cs extension
- ProjectReferences must be valid relative paths

**State Transitions**: Immutable after construction.

---

### SolutionInfo

Represents a parsed .sln file with its projects.

| Field | Type | Description |
|-------|------|-------------|
| SolutionPath | string | Absolute path to .sln file |
| SolutionDirectory | string | Directory containing .sln |
| SolutionName | string | Name derived from file name |
| Projects | IReadOnlyList\<SolutionProject\> | Projects in solution |

**Validation Rules**:
- SolutionPath must exist and be a valid .sln file
- At least one project must be present

---

### SolutionProject

Represents a project entry in a solution file.

| Field | Type | Description |
|-------|------|-------------|
| ProjectGuid | Guid | Unique project identifier |
| ProjectName | string | Display name in solution |
| RelativePath | string | Relative path from solution to .csproj |
| ProjectTypeGuid | Guid | Type identifier (C# project, folder, etc.) |
| IsCSharpProject | bool | True if ProjectTypeGuid indicates C# |

---

### SourceFileResult

Result of source file discovery for a project.

| Field | Type | Description |
|-------|------|-------------|
| IncludedFiles | IReadOnlyList\<string\> | Files matching include patterns |
| ExcludedFiles | IReadOnlyList\<string\> | Files matching exclude patterns |
| DiscoveryTime | TimeSpan | Time taken for discovery |
| GlobPatternsUsed | IReadOnlyList\<string\> | Patterns applied |

---

### LoadingResult

Result of workspace loading operation.

| Field | Type | Description |
|-------|------|-------------|
| Success | bool | True if loading completed |
| UsedFallback | bool | True if Buildalyzer was used |
| FallbackReason | string? | Why fallback was triggered |
| LoadTime | TimeSpan | Total loading time |
| ProjectCount | int | Number of projects loaded |
| SourceFileCount | int | Total source files loaded |
| Diagnostics | IReadOnlyList\<DiagnosticMessage\> | Warnings and errors |

---

### DiagnosticMessage

A diagnostic message from the loading process.

| Field | Type | Description |
|-------|------|-------------|
| Severity | DiagnosticSeverity | Error, Warning, or Info |
| Message | string | Human-readable message |
| FilePath | string? | Related file path, if applicable |
| Code | string? | Diagnostic code for programmatic handling |

**Enum DiagnosticSeverity**: Error, Warning, Info

---

## Relationships

```
SolutionInfo
    └── SolutionProject[] (1:N)
            └── ProjectInfo (resolved lazily, 1:1)
                    └── SourceFiles (1:N, discovered via globs)
                    └── ProjectReferences (1:N, resolved to ProjectInfo)

LoadingResult
    └── DiagnosticMessage[] (1:N)
```

## Entity Lifecycle

1. **SolutionInfo/ProjectInfo**: Created during parsing, immutable thereafter
2. **SourceFileResult**: Created during source discovery, discarded after files loaded
3. **LoadingResult**: Created after loading completes, returned to caller
4. **DiagnosticMessage**: Created throughout process, accumulated in result

## Design Notes

### Immutability

All entities are immutable after construction to:
- Enable safe concurrent access during parallel project loading
- Simplify reasoning about state
- Match existing Roslyn patterns

### Lazy Resolution

- `SolutionProject.ProjectInfo` is resolved lazily to avoid loading projects that may not be needed
- `ProjectInfo.ProjectReferences` stores paths, resolved to `ProjectInfo` on demand

### Memory Efficiency

- Source file paths are stored as strings (not loaded content)
- Roslyn `SyntaxTree` instances are created on-demand
- `SemanticModel` instances are created only when templates request type information
