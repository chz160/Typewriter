---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
status: 'complete'
completedAt: '2026-01-10'
inputDocuments:
  - _bmad-output/planning-artifacts/prd.md
  - docs/project-documentation/index.md
  - docs/project-documentation/project-overview.md
  - docs/project-documentation/architecture.md
  - docs/project-documentation/source-tree-analysis.md
  - docs/project-documentation/development-guide.md
workflowType: 'architecture'
project_name: 'Typewriter CLI'
user_name: 'Noah'
date: '2026-01-10'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Project Context Analysis

### Requirements Overview

**Functional Requirements:**
The PRD defines 39 functional requirements spanning:
- **Code Generation (FR1-7):** Core template processing, identical output to VS extension, custom C# code blocks, reference resolution
- **Project Discovery (FR8-12):** Solution/project path specification, automatic template discovery, reference resolution
- **Output & Diagnostics (FR13-19):** File generation summaries, error messages with file:line:column, warning vs error distinction
- **Configuration (FR20-30):** CLI arguments for MVP, `.typewriterrc` config files for Growth phase
- **Scripting Integration (FR35-39):** Exit codes, non-interactive operation, stdout/stderr separation

**Non-Functional Requirements:**
| Category | Key Requirements |
|----------|------------------|
| Performance | <3s cold start, <500ms/file, <30s solution load, <2GB memory |
| Reliability | Deterministic output, atomic writes, graceful error handling |
| Maintainability | ≥60% code reuse, clean project boundaries, provider pattern preservation |
| Compatibility | .NET Framework 4.7.2, byte-identical output parity, VS 2019/2022/2025 solution format |

**Scale & Complexity:**
- Primary domain: CLI Developer Tool
- Complexity level: Low-Medium
- Estimated new components: 6-8 (CLI entry point, workspace provider, template finder, console logger, settings, path resolver, orchestrator)
- Code reuse target: >90% of generation logic from existing codebase

### Technical Constraints & Dependencies

**Hard Constraints:**
1. **.NET Framework 4.7.2** - Must match existing codebase for assembly compatibility
2. **Output Parity** - Byte-identical TypeScript generation required
3. **Zero VS Dependencies** - CLI execution path must not require Visual Studio
4. **Generic Naming** - Avoid build-system-specific naming (per PRD future-proofing guidance)

**Dependencies:**
- Existing assemblies: `Typewriter.CodeModel`, `Typewriter.Metadata`, `Typewriter.Metadata.Roslyn`
- Microsoft.CodeAnalysis (Roslyn) - Same version as VS extension (4.14.0)
- Standalone Roslyn workspace for solution/project loading

### Cross-Cutting Concerns Identified

1. **Workspace Abstraction** - Replace `VisualStudioWorkspace` with standalone implementation while preserving `IMetadataProvider` contract
2. **Error Handling Strategy** - Consistent error/warning distinction across all components with file:line:column formatting
3. **Configuration Resolution** - CLI args > config file > defaults precedence across all operations
4. **Logging Abstraction** - Console output replacing VS Output Window, with verbosity levels
5. **Path Resolution** - Relative path handling without DTE project enumeration

## Starter Template Evaluation

### Primary Technology Domain

**CLI Tool Extension** - Extending existing .NET Framework 4.7.2 Visual Studio extension with command-line interface.

### Starter Options Considered

This is a **brownfield extension project**, not a greenfield starter template scenario. The "starter" is the existing Typewriter codebase.

| Approach | Description | Fit |
|----------|-------------|-----|
| Existing Codebase | Add new CLI project to existing solution | **Selected** |
| Fork & Refactor | Create separate CLI-only fork | Rejected - duplicates code |
| Shared Library Extract | Extract shared code to separate package | Overkill for scope |

### Selected Approach: New Project in Existing Solution

**Rationale:**
- Maximizes code reuse (>90% of generation logic)
- Single solution maintains consistency
- Shared test infrastructure
- Aligned with PRD requirement for minimal existing changes

**Initialization:**
```bash
# Add new console application to existing solution
dotnet new console -n Typewriter.CLI -f net472 -o src/CLI
dotnet sln Typewriter.sln add src/CLI/Typewriter.CLI.csproj
```

### Architectural Decisions Inherited from Existing Codebase

**Language & Runtime:**
- C# with .NET Framework 4.7.2
- Same Roslyn version (4.14.0) as VS extension

**Code Organization:**
- Provider pattern for metadata abstraction
- `*Impl.cs` naming for implementation wrappers
- Lazy-loaded collections for memory efficiency

**Testing Framework:**
- xUnit with Should fluent assertions
- NSubstitute for mocking
- MefHostingFixture pattern for DI testing

**Build Tooling:**
- MSBuild via Visual Studio solution
- Existing Directory.Build.props for shared configuration

## Core Architectural Decisions

### Decision Priority Analysis

**Critical Decisions (Block Implementation):**
- CLI argument parsing library
- Standalone workspace provider approach

**Important Decisions (Shape Architecture):**
- Configuration file format
- Console output strategy
- Error handling patterns

**Deferred Decisions (Post-MVP):**
- Watch mode implementation
- NuGet packaging strategy
- Cross-platform considerations

### CLI Infrastructure

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Argument Parsing | System.CommandLine | Official Microsoft library, better upstream PR acceptance |
| Configuration Format | Newtonsoft.Json | Likely already in solution, battle-tested |

### Workspace Provider

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Workspace Implementation | AdhocWorkspace + Buildalyzer | Buildalyzer already in solution as submodule, purpose-built for analyzing projects outside VS |

### Output & Error Handling

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Console Output | Plain text with ANSI colors | Zero dependencies, native terminal support, better UX |
| Error Format | Compiler-style (File:Line:Column: Message) | Familiar pattern, IDE-parseable, aligns with PRD FR15-17 |
| Exit Codes | 0=success, 1=generation failure, 2=invalid args | Per PRD specification |
| Stream Separation | Errors to stderr, Output to stdout | Standard CLI convention, scriptable |

### Decision Impact Analysis

**Implementation Sequence:**
1. Project setup with System.CommandLine reference
2. Workspace provider using Buildalyzer + AdhocWorkspace
3. Console output utilities with ANSI color support
4. Error handling infrastructure
5. Core generate command wiring

**Cross-Component Dependencies:**
- Workspace provider feeds into existing `IMetadataProvider` contract
- Error handling used by all components (workspace, template engine, file operations)
- Console output wraps all user-facing messages

## Implementation Patterns & Consistency Rules

### Inherited Patterns (From Existing Codebase)

**These patterns are already established - all CLI code MUST follow them:**

| Category | Pattern | Example |
|----------|---------|---------|
| Implementation Wrappers | `*Impl` suffix | `ClassImpl.cs`, `FileImpl.cs` |
| Metadata Interfaces | `I*Metadata` prefix | `IClassMetadata`, `IFileMetadata` |
| Collections | `*Collection` suffix with lazy loading | `ClassCollectionImpl` |
| Static Factories | `FromMetadata()` methods | `ClassImpl.FromMetadata(...)` |
| Null Handling | Null-coalescing to empty | `Name ?? string.Empty` |
| Lazy Properties | `_field ?? (_field = ...)` | Deferred initialization |

### CLI-Specific Patterns

**New patterns established for CLI project:**

| Category | Pattern | Example |
|----------|---------|---------|
| Command Classes | `*Command` suffix | `GenerateCommand.cs` |
| Console Output | `ConsoleOutput` static class | `ConsoleOutput.Success("Done")` |
| Metadata Provider | `CliMetadataProvider` | Standalone workspace provider |
| Configuration | `CliSettings` | CLI tool configuration |
| Test Location | `src/Tests/CLI/` subfolder | Co-located with existing tests |

### Naming Conventions

**File Naming:**
- Commands: `{Verb}Command.cs` (e.g., `GenerateCommand.cs`)
- Providers: `Cli{Purpose}Provider.cs` (e.g., `CliMetadataProvider.cs`)
- Utilities: `{Purpose}.cs` (e.g., `ConsoleOutput.cs`, `TemplateFinder.cs`)

**Class Naming:**
- Follow existing PascalCase convention
- Suffix indicates role: `*Command`, `*Provider`, `*Settings`

### Console Output Patterns

**Color Usage:**

| Level | Color | Usage |
|-------|-------|-------|
| Success | Green | Completion messages, file counts |
| Error | Red | Fatal errors, validation failures |
| Warning | Yellow | Non-fatal issues, skipped files |
| Info | Default | Normal output, file lists |

**Output Format:**
```
Typewriter CLI v1.0.0
Processing solution: ./MyProject.sln
Found 3 templates

Generated 12 TypeScript files in 1.2s
```

**Error Format:**
```
Error: Template compilation failed
  CustomerModel.tst:15:8: Cannot resolve type 'OrderStatus'
```

### Error Handling Patterns

**Exception Strategy:**
- Catch exceptions at command handler level
- Convert to user-friendly messages with file:line:column when available
- Write errors to stderr
- Return appropriate exit code (1 for generation failure, 2 for invalid args)

**Warning vs Error:**
- Warnings: Log and continue (e.g., file not in solution)
- Errors: Log and halt (e.g., template compilation failure)

### Enforcement Guidelines

**All AI Agents MUST:**
1. Follow existing `*Impl` pattern for any new implementation wrappers
2. Use `CliMetadataProvider` (not MSBuild-specific naming)
3. Place CLI tests in `src/Tests/CLI/` subfolder
4. Use `ConsoleOutput` for all user-facing messages
5. Follow compiler-style error format (File:Line:Column: Message)

**Anti-Patterns to Avoid:**
- Creating new test projects for CLI tests
- Using `Console.WriteLine` directly (use `ConsoleOutput`)
- MSBuild-specific naming in provider classes
- Mixing stdout and stderr for error messages

## Project Structure & Boundaries

### Complete Project Directory Structure

**Existing Solution (Context):**
```
Typewriter/
├── Typewriter.sln
├── Directory.Build.props
├── CLAUDE.md
├── README.md
├── Buildalyzer/                    # Git submodule
├── src/
│   ├── CodeModel/                  # Typewriter.CodeModel.dll (reuse)
│   ├── Metadata/                   # Typewriter.Metadata.dll (reuse)
│   ├── Roslyn/                     # Typewriter.Metadata.Roslyn.dll (reuse)
│   ├── Typewriter/                 # VS extension (unchanged)
│   ├── Tests/                      # Existing tests
│   └── ItemTemplates/              # VS item templates
└── docs/
```

**New CLI Project Structure:**
```
src/CLI/                            # NEW - Typewriter.CLI project
├── Typewriter.CLI.csproj
├── Program.cs                      # Entry point, System.CommandLine setup
├── Commands/
│   └── GenerateCommand.cs          # Main generate command
├── Infrastructure/
│   ├── CliMetadataProvider.cs      # IMetadataProvider implementation
│   ├── ConsoleOutput.cs            # ANSI-colored console output
│   ├── TemplateFinder.cs           # .tst file discovery
│   └── PathResolver.cs             # Relative path resolution
├── Configuration/
│   └── CliSettings.cs              # Configuration model (Growth phase)
└── Properties/
    └── AssemblyInfo.cs
```

**Test Additions:**
```
src/Tests/
├── CLI/                            # NEW - CLI test subfolder
│   ├── GenerateCommandTests.cs
│   ├── CliMetadataProviderTests.cs
│   ├── TemplateFinderTests.cs
│   └── ConsoleOutputTests.cs
└── ... (existing tests unchanged)
```

### Architectural Boundaries

**Assembly Dependencies:**
```
┌─────────────────────────────────────────────────────────────┐
│                    Typewriter.CLI                            │
│  (NEW - Console Application)                                 │
├─────────────────────────────────────────────────────────────┤
│  References:                                                 │
│  ├── Typewriter.CodeModel         (existing, unchanged)      │
│  ├── Typewriter.Metadata          (existing, unchanged)      │
│  ├── Typewriter.Metadata.Roslyn   (existing, unchanged)      │
│  ├── System.CommandLine           (new dependency)           │
│  ├── Newtonsoft.Json              (likely existing)          │
│  └── Buildalyzer                  (existing submodule)       │
└─────────────────────────────────────────────────────────────┘
```

**Provider Boundary:**
```
┌─────────────────────────┐     ┌─────────────────────────┐
│   VS Extension          │     │   CLI                    │
│   (Typewriter.dll)      │     │   (Typewriter.CLI.exe)   │
├─────────────────────────┤     ├─────────────────────────┤
│ RoslynMetadataProvider  │     │ CliMetadataProvider      │
│ └─VisualStudioWorkspace │     │ └─AdhocWorkspace         │
└──────────┬──────────────┘     └──────────┬──────────────┘
           │                               │
           └───────────┬───────────────────┘
                       ▼
           ┌─────────────────────────┐
           │  IMetadataProvider      │
           │  (shared contract)      │
           └─────────────────────────┘
                       │
                       ▼
           ┌─────────────────────────┐
           │  Template Engine        │
           │  (Parser, Compiler)     │
           │  100% shared            │
           └─────────────────────────┘
```

### Requirements to Structure Mapping

**MVP Functional Requirements:**

| Requirement | Implementation Location |
|-------------|------------------------|
| FR1-7: Code Generation | Reuse existing `Typewriter/Generation/` |
| FR8-12: Project Discovery | `CLI/Infrastructure/TemplateFinder.cs` |
| FR13-19: Output & Diagnostics | `CLI/Infrastructure/ConsoleOutput.cs` |
| FR20-24: CLI Arguments | `CLI/Commands/GenerateCommand.cs` |
| FR35-39: Scripting Integration | `CLI/Program.cs` (exit codes) |

**Growth Phase:**

| Feature | Implementation Location |
|---------|------------------------|
| Config file support | `CLI/Configuration/CliSettings.cs` |
| Verbosity flags | `CLI/Commands/GenerateCommand.cs` |

### Integration Points

**Internal Communication:**
- `GenerateCommand` → `CliMetadataProvider` → `IMetadataProvider` contract
- `CliMetadataProvider` → Buildalyzer → AdhocWorkspace → Solution/Project loading
- `CliMetadataProvider` → existing `RoslynFileMetadata`, `RoslynClassMetadata`, etc.
- Template engine receives `IFileMetadata` and produces TypeScript (unchanged)

**Data Flow:**
```
CLI Args → GenerateCommand
              │
              ▼
         TemplateFinder ─────► .tst files
              │
              ▼
      CliMetadataProvider
              │
         Buildalyzer
              │
              ▼
        AdhocWorkspace ─────► .sln/.csproj
              │
              ▼
      RoslynFileMetadata ────► C# source files
              │
              ▼
        Template Engine
              │
              ▼
      TypeScript Output ─────► .ts files
              │
              ▼
       ConsoleOutput ────────► stdout/stderr
```

### File Organization Patterns

**New Files Summary:**

| File | Purpose | Lines (est.) |
|------|---------|--------------|
| `Program.cs` | Entry point, command setup | ~50 |
| `GenerateCommand.cs` | Command definition & handler | ~150 |
| `CliMetadataProvider.cs` | Workspace + provider | ~100 |
| `ConsoleOutput.cs` | Colored output utilities | ~80 |
| `TemplateFinder.cs` | .tst file discovery | ~60 |
| `PathResolver.cs` | Path resolution utilities | ~40 |
| `CliSettings.cs` | Configuration model | ~30 |
| **Total new code** | | **~510 lines** |

**Reused from Existing:**
- `Typewriter.CodeModel` - 100%
- `Typewriter.Metadata` - 100%
- `Typewriter.Metadata.Roslyn` - 100% (metadata classes)
- `Typewriter/Generation/Parser.cs` - 100%
- `Typewriter/Generation/TemplateCodeParser.cs` - 100%
- `Typewriter/Generation/ItemFilter.cs` - 100%
- `Typewriter/CodeModel/Implementation/*` - 100%

## Architecture Validation Results

### Coherence Validation

**Decision Compatibility:** All technology choices are compatible with .NET Framework 4.7.2:
- System.CommandLine supports .NET Framework
- Newtonsoft.Json is widely used in .NET Framework projects
- Buildalyzer is designed for .NET Framework analysis
- Roslyn 4.14.0 matches existing extension

**Pattern Consistency:** CLI patterns extend existing codebase conventions:
- `CliMetadataProvider` follows `*Provider` naming
- `GenerateCommand` follows established command patterns
- Test organization co-located with existing tests

**Structure Alignment:** Project structure follows existing solution patterns:
- `src/CLI/` mirrors `src/CodeModel/`, `src/Roslyn/` conventions
- Single solution maintains build coherence
- Shared test infrastructure preserved

### Requirements Coverage Validation

**Functional Requirements Coverage:**

| Category | Requirements | Coverage |
|----------|--------------|----------|
| Code Generation | FR1-7 | 100% via existing template engine |
| Project Discovery | FR8-12 | 100% via TemplateFinder, CliMetadataProvider |
| Output & Diagnostics | FR13-19 | 100% via ConsoleOutput |
| CLI Arguments MVP | FR20-24 | 100% via System.CommandLine |
| Config Files Growth | FR25-30 | Designed via CliSettings |
| Scripting Integration | FR35-39 | 100% via exit codes |

**Non-Functional Requirements Coverage:**

| NFR | Architectural Support |
|-----|----------------------|
| Performance (<3s cold start) | Minimal new code, reuse existing compiled assemblies |
| Reliability (deterministic) | Same template engine guarantees identical output |
| Maintainability (≥60% reuse) | ~90%+ code reuse achieved |
| Compatibility (.NET 4.7.2) | Explicit constraint in all decisions |

### Implementation Readiness Validation

**Decision Completeness:** All critical decisions documented:
- CLI argument parsing: System.CommandLine
- Workspace provider: AdhocWorkspace + Buildalyzer
- Configuration: Newtonsoft.Json
- Output: ANSI colors, compiler-style errors
- Exit codes: 0/1/2 per PRD

**Structure Completeness:** Full project tree defined:
- 7 new source files specified
- Test subfolder defined
- Integration boundaries clear

**Pattern Completeness:** All conflict points addressed:
- Naming conventions established
- Error handling patterns defined
- Console output patterns specified

### Gap Analysis Results

**Critical Gaps:** None - architecture is complete for MVP scope

**Post-MVP Considerations:**
- Watch mode requires file system monitoring architecture
- NuGet packaging requires distribution decisions
- Cross-platform may require .NET 6+ migration planning

### Architecture Completeness Checklist

**Requirements Analysis**
- [x] Project context thoroughly analyzed
- [x] Scale and complexity assessed (Low-Medium)
- [x] Technical constraints identified (.NET 4.7.2, output parity)
- [x] Cross-cutting concerns mapped (5 concerns)

**Architectural Decisions**
- [x] Critical decisions documented with rationale
- [x] Technology stack fully specified
- [x] Integration patterns defined (IMetadataProvider)
- [x] Performance considerations addressed

**Implementation Patterns**
- [x] Naming conventions established
- [x] Structure patterns defined
- [x] Communication patterns specified
- [x] Process patterns documented (error handling)

**Project Structure**
- [x] Complete directory structure defined
- [x] Component boundaries established
- [x] Integration points mapped
- [x] Requirements to structure mapping complete

### Architecture Readiness Assessment

**Overall Status:** READY FOR IMPLEMENTATION

**Confidence Level:** High - brownfield extension with clear boundaries

**Key Strengths:**
- Massive code reuse (~90%+) minimizes risk
- Clear provider pattern boundary enables clean separation
- Existing test infrastructure reusable
- Official Microsoft dependencies improve PR acceptance

**Areas for Future Enhancement:**
- Watch mode architecture (Growth phase)
- NuGet distribution packaging
- Performance profiling integration

### Implementation Handoff

**AI Agent Guidelines:**
- Follow all architectural decisions exactly as documented
- Use implementation patterns consistently across all components
- Respect project structure and boundaries
- Use `ConsoleOutput` for all user-facing messages
- Follow compiler-style error format

**First Implementation Priority:**
1. Create `src/CLI/Typewriter.CLI.csproj` with references
2. Implement `ConsoleOutput` utility class
3. Implement `CliMetadataProvider`
4. Wire up `GenerateCommand` with System.CommandLine
5. Add CLI tests in `src/Tests/CLI/`

## Architecture Completion Summary

### Workflow Completion

**Architecture Decision Workflow:** COMPLETED
**Total Steps Completed:** 8
**Date Completed:** 2026-01-10
**Document Location:** `_bmad-output/planning-artifacts/architecture.md`

### Final Architecture Deliverables

**Complete Architecture Document**
- All architectural decisions documented with specific versions
- Implementation patterns ensuring AI agent consistency
- Complete project structure with all files and directories
- Requirements to architecture mapping
- Validation confirming coherence and completeness

**Implementation Ready Foundation**
- 5 core architectural decisions made
- 11 implementation patterns defined (6 inherited + 5 CLI-specific)
- 7 new source files specified
- 39 functional requirements fully supported

**AI Agent Implementation Guide**
- Technology stack with verified versions
- Consistency rules that prevent implementation conflicts
- Project structure with clear boundaries
- Integration patterns and communication standards

### Development Sequence

1. Initialize `src/CLI/Typewriter.CLI.csproj` with project references
2. Set up assembly references to existing projects
3. Implement infrastructure components (`ConsoleOutput`, `CliMetadataProvider`)
4. Build command structure with System.CommandLine
5. Wire template engine integration via `IMetadataProvider`
6. Add tests in `src/Tests/CLI/`

### Quality Assurance Checklist

**Architecture Coherence**
- [x] All decisions work together without conflicts
- [x] Technology choices are compatible (.NET 4.7.2)
- [x] Patterns support the architectural decisions
- [x] Structure aligns with existing solution

**Requirements Coverage**
- [x] All functional requirements are supported
- [x] All non-functional requirements are addressed
- [x] Cross-cutting concerns are handled
- [x] Integration points are defined

**Implementation Readiness**
- [x] Decisions are specific and actionable
- [x] Patterns prevent agent conflicts
- [x] Structure is complete and unambiguous
- [x] Examples are provided for clarity

---

**Architecture Status:** READY FOR IMPLEMENTATION

**Next Phase:** Begin implementation using the architectural decisions and patterns documented herein.

**Document Maintenance:** Update this architecture when major technical decisions are made during implementation.

