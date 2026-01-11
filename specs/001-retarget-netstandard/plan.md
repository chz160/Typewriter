# Implementation Plan: Retarget CodeModel and Metadata to .NET Standard 2.0

**Branch**: `001-retarget-netstandard` | **Date**: 2026-01-11 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/001-retarget-netstandard/spec.md`

## Summary

Retarget the `Typewriter.CodeModel` and `Typewriter.Metadata` projects from .NET Framework
4.7.2 to .NET Standard 2.0 to enable cross-platform consumption. This involves converting
both projects from legacy .csproj format to SDK-style format while maintaining backward
compatibility with the existing VS extension and dependent projects.

## Technical Context

**Language/Version**: C# (latest language version supported by .NET Standard 2.0)
**Primary Dependencies**: None for CodeModel; CodeModel reference for Metadata
**Storage**: N/A (library projects, no data persistence)
**Testing**: xUnit with Should assertions, vstest.console.exe or dotnet test
**Target Platform**: .NET Standard 2.0 (consumable by net472 and net8.0+)
**Project Type**: Library projects within existing VS extension solution
**Performance Goals**: No performance regression from current implementation
**Constraints**: Must remain compatible with .NET Framework 4.7.2 consumers (VS extension)
**Scale/Scope**: 2 projects, ~50 source files total, ~20 dependent references

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Upstream Style Conformance | PASS | No code changes required; only project file updates |
| II. Test Coverage Requirements | PASS | Existing tests verify functionality; no new tests needed for retargeting |
| III. Architectural Consistency | PASS | Three-layer architecture preserved; no structural changes |
| IV. VS Extension Compatibility | PASS | netstandard2.0 assemblies consumable by net472 projects |
| V. Performance and Resource Efficiency | PASS | No runtime changes; SDK-style may improve build performance |

**Upstream Compatibility:**
- PASS: No public API changes; assembly names and namespaces preserved
- PASS: Existing .tst templates continue to work unchanged
- PASS: Dual-naming pattern preserved (no code modifications)

## Project Structure

### Documentation (this feature)

```text
specs/001-retarget-netstandard/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── quickstart.md        # Phase 1 output
└── checklists/
    └── requirements.md  # Specification quality checklist
```

### Source Code (repository root)

```text
src/
├── CodeModel/
│   ├── Typewriter.CodeModel.csproj    # MODIFIED: SDK-style, netstandard2.0
│   ├── Properties/
│   │   └── AssemblyInfo.cs            # PRESERVED: via GenerateAssemblyInfo=false
│   ├── CodeModel/                     # UNCHANGED: abstract types
│   ├── Attributes/                    # UNCHANGED
│   ├── Configuration/                 # UNCHANGED
│   ├── Extensions/                    # UNCHANGED
│   └── VisualStudio/                  # UNCHANGED
│
├── Metadata/
│   ├── Typewriter.Metadata.csproj     # MODIFIED: SDK-style, netstandard2.0
│   ├── Properties/
│   │   └── AssemblyInfo.cs            # PRESERVED: via GenerateAssemblyInfo=false
│   ├── Interfaces/                    # UNCHANGED: metadata interfaces
│   └── Providers/                     # UNCHANGED
│
├── Roslyn/                            # UNCHANGED: remains net472, references retargeted libs
├── Typewriter/                        # UNCHANGED: VS extension, remains net472
└── Tests/                             # UNCHANGED: verifies no regressions
```

**Structure Decision**: No structural changes. Only `.csproj` file contents are modified
to enable netstandard2.0 targeting with SDK-style format.

## Complexity Tracking

> No constitution violations requiring justification. This is a straightforward
> project file conversion with no architectural changes.

| Aspect | Complexity | Justification |
|--------|------------|---------------|
| Files changed | 2 | Only .csproj files modified |
| Code changes | 0 | No source code modifications |
| Risk level | Low | SDK-style + netstandard2.0 is well-established pattern |
