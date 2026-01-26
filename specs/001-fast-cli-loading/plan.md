# Implementation Plan: Fast CLI Project Loading

**Branch**: `001-fast-cli-loading` | **Date**: 2026-01-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-fast-cli-loading/spec.md`

## Summary

Optimize CLI project loading performance from 2+ minutes to under 5 seconds for single projects (700 files) by replacing Buildalyzer/MSBuild-based project loading with direct Roslyn parsing. The new approach parses .csproj/.sln files as XML to discover source files, uses glob pattern matching for SDK-style projects, and creates CSharpCompilation directly using Roslyn APIs. The existing Buildalyzer approach is retained as a fallback when fast loading fails.

## Technical Context

**Language/Version**: C# 12 / .NET 8.0 (CLI project), .NET Framework 4.7.2 (VS extension - unchanged)
**Primary Dependencies**: Microsoft.CodeAnalysis.CSharp.Workspaces 4.14.0, Microsoft.Extensions.FileSystemGlobbing 9.0.0, System.CommandLine 2.0.0-beta4
**Storage**: N/A (file system only)
**Testing**: xUnit with Should assertions, NSubstitute for mocking
**Target Platform**: Windows, Linux, macOS (cross-platform CLI via .NET 8)
**Project Type**: Single project (CLI enhancement within existing solution)
**Performance Goals**: 700 files in <5 seconds (single project), <30 seconds (solution)
**Constraints**: <1GB memory for typical solutions, must maintain template compatibility
**Scale/Scope**: Projects with 700+ source files, solutions with 50+ projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Upstream Style Conformance | PASS | New classes follow existing naming: `Direct*` prefix for new workspace, `*Parser` for parsers |
| II. Test Coverage Requirements | PASS | Plan includes unit tests for parsers and integration tests for workspace loading |
| III. Architectural Consistency | PASS | New code follows existing three-layer pattern; metadata interfaces unchanged |
| IV. Visual Studio Extension Compatibility | PASS | Changes are CLI-only; VS extension code unchanged |
| V. Performance and Resource Efficiency | PASS | Core purpose is performance optimization with lazy loading |
| Upstream Compatibility | PASS | Template API unchanged; generated output byte-identical |

**Gate Result**: PASS - No violations requiring justification.

**Post-Design Re-check (Phase 1 complete)**:
- All new classes use appropriate naming (`ProjectFileParser`, `SolutionFileParser`, `SourceFileDiscovery`, `ReferenceResolver`, `DirectRoslynWorkspace`)
- Test coverage planned for all new components
- Architecture unchanged - only CLI infrastructure modified
- VS extension code untouched
- Performance is the core objective with lazy semantic model loading
- Template compatibility verified through data model - no API changes

## Project Structure

### Documentation (this feature)

```text
specs/001-fast-cli-loading/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (internal interfaces)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/CLI/
├── Infrastructure/
│   ├── Models/                         # NEW: Data model classes
│   │   ├── ProjectInfo.cs
│   │   ├── SolutionInfo.cs
│   │   ├── SourceFileResult.cs
│   │   └── LoadingResult.cs
│   ├── CliRoslynWorkspace.cs           # MODIFY: Add fallback orchestration
│   ├── DirectRoslynWorkspace.cs        # NEW: Fast loading workspace
│   ├── ProjectFileParser.cs            # NEW: .csproj XML parsing
│   ├── SolutionFileParser.cs           # NEW: .sln file parsing
│   ├── SourceFileDiscovery.cs          # NEW: Glob pattern matching
│   └── ReferenceResolver.cs            # NEW: Runtime assembly resolution
├── Commands/
│   └── GenerateCommand.cs              # MODIFY: Add timing/verbose output
└── Configuration/
    └── CliSettings.cs                  # No changes needed

src/Tests/
├── CLI/
│   ├── ProjectFileParserTests.cs       # NEW: Unit tests for parser
│   ├── SolutionFileParserTests.cs      # NEW: Unit tests for parser
│   ├── SourceFileDiscoveryTests.cs     # NEW: Unit tests for glob matching
│   ├── DirectRoslynWorkspaceTests.cs   # NEW: Integration tests
│   └── FallbackBehaviorTests.cs        # NEW: Fallback scenario tests
└── TestProjects/                       # NEW: Test fixtures
    ├── SdkStyleProject/
    ├── LegacyProject/
    └── MixedSolution/
```

**Structure Decision**: Single project enhancement. All new code goes in `src/CLI/Infrastructure/` following the existing pattern. Test projects added under `src/Tests/CLI/` with test fixture projects.

## Complexity Tracking

> No violations to justify - design follows constitution principles.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | N/A | N/A |
