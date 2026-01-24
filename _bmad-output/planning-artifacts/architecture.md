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
1. **Multi-Target Architecture** - Solution must support both .NET Framework 4.7.2 (VS extension) and .NET 8 (CLI)
2. **.NET Standard 2.0 Shared Library** - New `Typewriter.Core` project enables code sharing between legacy VS extension and modern CLI
3. **Output Parity** - Byte-identical TypeScript generation required
4. **Zero VS Dependencies** - CLI execution path must not require Visual Studio
5. **Generic Naming** - Avoid build-system-specific naming (per PRD future-proofing guidance)
6. **DRY Principle** - Maximum code reuse via shared library extraction

**Framework Strategy:**
| Project | Target Framework | Status | Rationale |
|---------|-----------------|--------|-----------|
| Typewriter (VS Extension) | .NET Framework 4.7.2 | Unchanged | VS extension requirement |
| Typewriter.CodeModel | .NET Standard 2.0 | **COMPLETED** | Retargeted - consumable by net472 and net8.0 |
| Typewriter.Metadata | .NET Standard 2.0 | **COMPLETED** | Retargeted - consumable by net472 and net8.0 |
| Typewriter.Metadata.Roslyn | .NET Framework 4.7.2 | Unchanged | VS-specific Roslyn workspace (cannot migrate) |
| **Typewriter.Core (NEW)** | .NET Standard 2.0 | Planned | Shared generation engine extraction |
| **Typewriter.CLI (NEW)** | .NET 8 | Planned | Modern runtime, cross-platform ready |

**Dependencies:**
- Existing assemblies: `Typewriter.CodeModel`, `Typewriter.Metadata`, `Typewriter.Metadata.Roslyn`
- Microsoft.CodeAnalysis (Roslyn) - Same version as VS extension (4.14.0)
- Standalone Roslyn workspace for solution/project loading

### Cross-Cutting Concerns Identified

1. **Shared Library Extraction** - Identify and extract VS-independent code to `Typewriter.Core` (netstandard2.0) for maximum reuse
2. **Workspace Abstraction** - Replace `VisualStudioWorkspace` with standalone implementation while preserving `IMetadataProvider` contract
3. **Error Handling Strategy** - Consistent error/warning distinction across all components with file:line:column formatting
4. **Configuration Resolution** - CLI args > config file > defaults precedence across all operations
5. **Logging Abstraction** - Console output replacing VS Output Window, with verbosity levels
6. **Path Resolution** - Relative path handling without DTE project enumeration
7. **Multi-Target Compatibility** - Ensure shared code compiles against netstandard2.0 APIs only

## Starter Template Evaluation

### Primary Technology Domain

**CLI Tool Extension** - Extending existing .NET Framework 4.7.2 Visual Studio extension with command-line interface.

### Starter Options Considered

This is a **brownfield extension project**, not a greenfield starter template scenario. The "starter" is the existing Typewriter codebase.

| Approach | Description | Fit |
|----------|-------------|-----|
| Existing Codebase + Shared Core | Add CLI project + extract shared code to netstandard2.0 library | **Selected** |
| Fork & Refactor | Create separate CLI-only fork | Rejected - duplicates code |
| Shared NuGet Package | Extract shared code to external NuGet package | Rejected - unnecessary distribution complexity |

### Selected Approach: Multi-Project Architecture with Shared Core

**Rationale:**
- Maximizes code reuse (>90% of generation logic) via shared `Typewriter.Core`
- Single solution maintains consistency
- netstandard2.0 bridges .NET Framework 4.7.2 and .NET 8
- Future-proofs for eventual full .NET migration
- Aligned with PRD requirement for minimal existing changes while enabling DRY principles

**Initialization:**
```bash
# Create shared core library targeting netstandard2.0
dotnet new classlib -n Typewriter.Core -f netstandard2.0 -o src/Core
dotnet sln Typewriter.sln add src/Core/Typewriter.Core.csproj

# Add new console application targeting .NET 8
dotnet new console -n Typewriter.CLI -f net8.0 -o src/CLI
dotnet sln Typewriter.sln add src/CLI/Typewriter.CLI.csproj
```

### Architectural Decisions Inherited from Existing Codebase

**Language & Runtime:**
- C# with multi-target support (.NET Framework 4.7.2, .NET Standard 2.0, .NET 8)
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

**New Typewriter.Core Project Structure (.NET Standard 2.0):**
```
src/Core/                           # NEW - Typewriter.Core shared library
├── Typewriter.Core.csproj          # Target: netstandard2.0
├── CodeModel/
│   ├── Interfaces/                 # Extracted from Typewriter.CodeModel
│   │   ├── IClass.cs
│   │   ├── IProperty.cs
│   │   ├── IMethod.cs
│   │   ├── IType.cs
│   │   └── ...                     # All code model interfaces
│   └── Extensions/                 # Type extension helpers
│       └── TypeExtensions.cs
├── Metadata/
│   ├── IMetadataProvider.cs        # Core provider contract
│   ├── IClassMetadata.cs
│   ├── IPropertyMetadata.cs
│   └── ...                         # All metadata interfaces
├── Generation/
│   ├── Parser.cs                   # Template parser (extracted)
│   ├── TemplateCodeParser.cs       # Code block parser (extracted)
│   ├── Compiler.cs                 # Template compiler (extracted)
│   ├── ItemFilter.cs               # Filter logic (extracted)
│   └── Template.cs                 # Core template logic (extracted)
└── Utilities/
    ├── PathUtilities.cs            # Path resolution helpers
    └── StringExtensions.cs         # Common string helpers
```

**Code Extraction Strategy for Typewriter.Core:**

| Source Project | Components to Extract | Extraction Notes |
|----------------|----------------------|------------------|
| Typewriter.CodeModel | All interfaces (`IClass`, `IProperty`, etc.) | Move to Core/CodeModel/Interfaces |
| Typewriter.CodeModel | Type extensions | Move to Core/CodeModel/Extensions |
| Typewriter.Metadata | All metadata interfaces | Move to Core/Metadata |
| Typewriter | Parser, Compiler, TemplateCodeParser | Remove VS dependencies first |
| Typewriter | ItemFilter | Already VS-independent |

**What Stays in Existing Projects:**
- `Typewriter.Metadata.Roslyn` - VS-specific Roslyn workspace integration
- `Typewriter/CodeModel/Implementation/*` - VS-specific `*Impl.cs` classes
- `Typewriter/VisualStudio/*` - All VS integration code
- `Typewriter/TemplateEditor/*` - VS editor features

**New CLI Project Structure (.NET 8):**
```
src/CLI/                            # NEW - Typewriter.CLI project
├── Typewriter.CLI.csproj           # Target: net8.0
├── Program.cs                      # Entry point, System.CommandLine setup
├── Commands/
│   └── GenerateCommand.cs          # Main generate command
├── Infrastructure/
│   ├── CliMetadataProvider.cs      # IMetadataProvider implementation
│   ├── CliRoslynWorkspace.cs       # Standalone Roslyn workspace
│   ├── ConsoleOutput.cs            # ANSI-colored console output
│   ├── TemplateFinder.cs           # .tst file discovery
│   └── PathResolver.cs             # Relative path resolution
├── Configuration/
│   └── CliSettings.cs              # Configuration model (Growth phase)
└── CodeModel/
    └── Implementation/             # CLI-specific *Impl.cs classes
        ├── CliClassImpl.cs
        ├── CliPropertyImpl.cs
        └── ...                     # Mirroring VS extension pattern
```

**Test Additions:**
```
src/Tests/
├── Core/                           # NEW - Core library tests
│   ├── ParserTests.cs
│   ├── CompilerTests.cs
│   └── TemplateTests.cs
├── CLI/                            # NEW - CLI test subfolder
│   ├── GenerateCommandTests.cs
│   ├── CliMetadataProviderTests.cs
│   ├── TemplateFinderTests.cs
│   └── ConsoleOutputTests.cs
└── ... (existing tests unchanged)
```

### Architectural Boundaries

**Assembly Dependencies - Multi-Target Architecture:**
```
┌───────────────────────────────────────────────────────────────────────────┐
│                         Typewriter.Core                                    │
│                    (NEW - .NET Standard 2.0)                               │
│  Shared between VS Extension and CLI                                       │
├───────────────────────────────────────────────────────────────────────────┤
│  Contains:                                                                 │
│  ├── CodeModel Interfaces (IClass, IProperty, IMethod, etc.)              │
│  ├── Metadata Interfaces (IClassMetadata, IPropertyMetadata, etc.)        │
│  ├── Template Engine (Parser, Compiler, TemplateCodeParser)               │
│  └── Common Utilities                                                      │
└───────────────────────────────────────────────────────────────────────────┘
                    ▲                               ▲
                    │                               │
┌───────────────────┴───────────────┐ ┌────────────┴────────────────────────┐
│     Typewriter (VS Extension)      │ │         Typewriter.CLI              │
│     (.NET Framework 4.7.2)         │ │         (.NET 8)                    │
├────────────────────────────────────┤ ├─────────────────────────────────────┤
│  References:                       │ │  References:                        │
│  ├── Typewriter.Core               │ │  ├── Typewriter.Core                │
│  ├── Typewriter.Metadata.Roslyn    │ │  ├── System.CommandLine             │
│  ├── VS SDK assemblies             │ │  ├── Newtonsoft.Json                │
│  └── EnvDTE                        │ │  └── Buildalyzer                    │
├────────────────────────────────────┤ ├─────────────────────────────────────┤
│  Contains:                         │ │  Contains:                          │
│  ├── VS-specific *Impl.cs classes  │ │  ├── CLI-specific *Impl.cs classes  │
│  ├── VisualStudio integration      │ │  ├── CliMetadataProvider            │
│  ├── TemplateEditor features       │ │  ├── CliRoslynWorkspace             │
│  └── RoslynMetadataProvider        │ │  └── ConsoleOutput                  │
└────────────────────────────────────┘ └─────────────────────────────────────┘
```

**Provider Boundary with Shared Core:**
```
┌─────────────────────────────┐     ┌─────────────────────────────┐
│   VS Extension              │     │   CLI                        │
│   (.NET Framework 4.7.2)    │     │   (.NET 8)                   │
├─────────────────────────────┤     ├─────────────────────────────┤
│ RoslynMetadataProvider      │     │ CliMetadataProvider          │
│ └─VisualStudioWorkspace     │     │ └─Buildalyzer+AdhocWorkspace │
└──────────┬──────────────────┘     └──────────┬──────────────────┘
           │                                   │
           └─────────────┬─────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Typewriter.Core                              │
│                   (.NET Standard 2.0)                            │
├─────────────────────────────────────────────────────────────────┤
│           ┌─────────────────────────┐                            │
│           │  IMetadataProvider      │                            │
│           │  (shared contract)      │                            │
│           └──────────┬──────────────┘                            │
│                      │                                           │
│                      ▼                                           │
│           ┌─────────────────────────┐                            │
│           │  Template Engine        │                            │
│           │  (Parser, Compiler)     │                            │
│           │  100% shared code       │                            │
│           └─────────────────────────┘                            │
└─────────────────────────────────────────────────────────────────┘
```

**Framework Compatibility Matrix:**
```
┌────────────────────────────┬──────────────────────┬─────────────────┬───────────┐
│       Assembly             │   Target Framework   │   Consumers     │  Status   │
├────────────────────────────┼──────────────────────┼─────────────────┼───────────┤
│ Typewriter.CodeModel       │ .NET Standard 2.0    │ VS Ext + CLI    │ DONE      │
│ Typewriter.Metadata        │ .NET Standard 2.0    │ VS Ext + CLI    │ DONE      │
│ Typewriter.Metadata.Roslyn │ .NET Framework 4.7.2 │ VS Extension    │ Unchanged │
│ Typewriter (VS Extension)  │ .NET Framework 4.7.2 │ Visual Studio   │ Unchanged │
│ Typewriter.Core (NEW)      │ .NET Standard 2.0    │ VS Ext + CLI    │ Planned   │
│ Typewriter.CLI (NEW)       │ .NET 8               │ Command Line    │ Planned   │
└────────────────────────────┴──────────────────────┴─────────────────┴───────────┘
```

### Requirements to Structure Mapping

**MVP Functional Requirements:**

| Requirement | Implementation Location |
|-------------|------------------------|
| FR1-7: Code Generation | `Core/Generation/` (shared template engine) |
| FR8-12: Project Discovery | `CLI/Infrastructure/TemplateFinder.cs` |
| FR13-19: Output & Diagnostics | `CLI/Infrastructure/ConsoleOutput.cs` |
| FR20-24: CLI Arguments | `CLI/Commands/GenerateCommand.cs` |
| FR35-39: Scripting Integration | `CLI/Program.cs` (exit codes) |

**Growth Phase:**

| Feature | Implementation Location |
|---------|------------------------|
| Config file support | `CLI/Configuration/CliSettings.cs` |
| Verbosity flags | `CLI/Commands/GenerateCommand.cs` |

**Shared Core Components (DRY):**

| Component | Source Location | Core Location | Consumers |
|-----------|-----------------|---------------|-----------|
| Code Model Interfaces | `CodeModel/*.cs` | `Core/CodeModel/Interfaces/` | VS + CLI |
| Metadata Interfaces | `Metadata/*.cs` | `Core/Metadata/` | VS + CLI |
| Template Parser | `Typewriter/Generation/Parser.cs` | `Core/Generation/Parser.cs` | VS + CLI |
| Template Compiler | `Typewriter/Generation/Compiler.cs` | `Core/Generation/Compiler.cs` | VS + CLI |
| Item Filter | `Typewriter/Generation/ItemFilter.cs` | `Core/Generation/ItemFilter.cs` | VS + CLI |

### Integration Points

**Internal Communication:**
- `GenerateCommand` → `CliMetadataProvider` → `IMetadataProvider` contract (from Core)
- `CliMetadataProvider` → Buildalyzer → AdhocWorkspace → Solution/Project loading
- `CliMetadataProvider` → CLI-specific `*Impl.cs` classes implementing Core interfaces
- Template engine (from Core) receives `IFileMetadata` and produces TypeScript

**Data Flow with Shared Core:**
```
CLI Args → GenerateCommand
              │
              ▼
         TemplateFinder ─────────────────────► .tst files
              │
              ▼
      CliMetadataProvider (CLI)
              │
         Buildalyzer
              │
              ▼
        AdhocWorkspace ──────────────────────► .sln/.csproj
              │
              ▼
      Cli*Impl classes (CLI) ────────────────► C# source files
              │
              │ implements
              ▼
┌─────────────────────────────────────────────────────────────┐
│                     Typewriter.Core                          │
│  ┌────────────────────┐    ┌────────────────────┐           │
│  │ IMetadataProvider  │───►│ Template Engine    │           │
│  │ IFileMetadata      │    │ (Parser, Compiler) │           │
│  │ IClassMetadata     │    └─────────┬──────────┘           │
│  └────────────────────┘              │                      │
└──────────────────────────────────────┼──────────────────────┘
                                       │
                                       ▼
                             TypeScript Output ──────────────► .ts files
                                       │
                                       ▼
                              ConsoleOutput (CLI) ───────────► stdout/stderr
```

### File Organization Patterns

**New Files Summary - Typewriter.Core:**

| File | Purpose | Lines (est.) |
|------|---------|--------------|
| `Typewriter.Core.csproj` | Project file | ~20 |
| `CodeModel/Interfaces/*.cs` | Code model interfaces (extracted) | ~500 |
| `Metadata/*.cs` | Metadata interfaces (extracted) | ~300 |
| `Generation/Parser.cs` | Template parser (extracted) | ~200 |
| `Generation/Compiler.cs` | Template compiler (extracted) | ~150 |
| `Generation/TemplateCodeParser.cs` | Code block parser (extracted) | ~100 |
| `Generation/ItemFilter.cs` | Filter logic (extracted) | ~50 |
| `Utilities/*.cs` | Common helpers | ~100 |
| **Core total (mostly extracted)** | | **~1420 lines** |

**New Files Summary - Typewriter.CLI:**

| File | Purpose | Lines (est.) |
|------|---------|--------------|
| `Typewriter.CLI.csproj` | Project file | ~30 |
| `Program.cs` | Entry point, command setup | ~50 |
| `Commands/GenerateCommand.cs` | Command definition & handler | ~150 |
| `Infrastructure/CliMetadataProvider.cs` | Workspace + provider | ~100 |
| `Infrastructure/CliRoslynWorkspace.cs` | Standalone Roslyn workspace | ~80 |
| `Infrastructure/ConsoleOutput.cs` | Colored output utilities | ~80 |
| `Infrastructure/TemplateFinder.cs` | .tst file discovery | ~60 |
| `Infrastructure/PathResolver.cs` | Path resolution utilities | ~40 |
| `Configuration/CliSettings.cs` | Configuration model | ~30 |
| `CodeModel/Implementation/Cli*Impl.cs` | CLI-specific implementations | ~300 |
| **CLI total (new code)** | | **~920 lines** |

**Code Movement Strategy (DRY):**

| Category | Action | Impact |
|----------|--------|--------|
| Interfaces | Extract to Core | ~800 lines moved, 0 new |
| Template Engine | Extract to Core | ~500 lines moved, 0 new |
| VS *Impl.cs | Stay in VS Extension | Unchanged |
| CLI *Impl.cs | New in CLI | ~300 lines new |
| CLI Infrastructure | New in CLI | ~620 lines new |

**Reused via Shared Core:**
- `Typewriter.Core/CodeModel/Interfaces/*` - 100% shared between VS + CLI
- `Typewriter.Core/Metadata/*` - 100% shared between VS + CLI
- `Typewriter.Core/Generation/*` - 100% shared between VS + CLI

## Architecture Validation Results

### Coherence Validation

**Decision Compatibility:** All technology choices support the multi-target architecture:
- .NET Standard 2.0 is compatible with both .NET Framework 4.7.2 and .NET 8
- System.CommandLine supports .NET 8
- Newtonsoft.Json supports both frameworks
- Buildalyzer supports .NET 8 runtime
- Roslyn 4.14.0 available for both targets

**Pattern Consistency:** CLI patterns extend existing codebase conventions:
- `CliMetadataProvider` follows `*Provider` naming
- `GenerateCommand` follows established command patterns
- Test organization co-located with existing tests
- Core library extraction follows DRY principles

**Structure Alignment:** Project structure follows existing solution patterns:
- `src/Core/` provides shared foundation
- `src/CLI/` mirrors `src/CodeModel/`, `src/Roslyn/` conventions
- Single solution maintains build coherence
- Shared test infrastructure preserved

**Framework Strategy Validation:**
- VS Extension stays on .NET Framework 4.7.2 (required for VS compatibility)
- CLI uses .NET 8 (modern runtime, cross-platform ready)
- Typewriter.Core bridges both via netstandard2.0

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
| Performance (<3s cold start) | .NET 8 CLI has faster startup than .NET Framework |
| Reliability (deterministic) | Same template engine (Core) guarantees identical output |
| Maintainability (≥60% reuse) | ~90%+ via shared Typewriter.Core library |
| Compatibility (multi-target) | VS stays .NET 4.7.2, CLI uses .NET 8, Core bridges via netstandard2.0 |
| Future-proofing | .NET 8 CLI ready for cross-platform expansion |

### Implementation Readiness Validation

**Decision Completeness:** All critical decisions documented:
- Multi-target framework strategy: Core (netstandard2.0), CLI (.NET 8), VS (.NET 4.7.2)
- CLI argument parsing: System.CommandLine
- Workspace provider: AdhocWorkspace + Buildalyzer
- Configuration: Newtonsoft.Json
- Output: ANSI colors, compiler-style errors
- Exit codes: 0/1/2 per PRD

**Structure Completeness:** Full project tree defined:
- 2 new projects (Core + CLI) fully specified
- Code extraction strategy defined for DRY compliance
- Test subfolders defined for both Core and CLI
- Integration boundaries clear

**Pattern Completeness:** All conflict points addressed:
- Naming conventions established
- Error handling patterns defined
- Console output patterns specified
- Shared vs project-specific code boundaries clear

### Gap Analysis Results

**Critical Gaps:** None - architecture is complete for MVP scope

**Post-MVP Considerations:**
- Watch mode requires file system monitoring architecture (simpler with .NET 8)
- NuGet packaging: CLI already .NET 8, ready for `dotnet tool` distribution
- Cross-platform: .NET 8 CLI is already cross-platform capable
- Future migration: VS extension could eventually target newer VS versions with .NET 6+

### Architecture Completeness Checklist

**Requirements Analysis**
- [x] Project context thoroughly analyzed
- [x] Scale and complexity assessed (Low-Medium)
- [x] Technical constraints identified (multi-target: .NET 4.7.2, netstandard2.0, .NET 8)
- [x] Cross-cutting concerns mapped (7 concerns including shared library extraction)

**Architectural Decisions**
- [x] Critical decisions documented with rationale
- [x] Multi-target framework strategy fully specified
- [x] Technology stack specified for each target framework
- [x] Integration patterns defined (IMetadataProvider via Core)
- [x] Performance considerations addressed (.NET 8 for CLI)

**Implementation Patterns**
- [x] Naming conventions established
- [x] Structure patterns defined
- [x] Communication patterns specified
- [x] Process patterns documented (error handling)
- [x] Code extraction strategy defined (DRY via Core)

**Project Structure**
- [x] Complete directory structure defined (Core + CLI)
- [x] Component boundaries established (shared vs specific)
- [x] Integration points mapped
- [x] Requirements to structure mapping complete
- [x] Framework compatibility matrix defined

### Architecture Readiness Assessment

**Overall Status:** READY FOR IMPLEMENTATION

**Confidence Level:** High - brownfield extension with clear boundaries and future-proof design

**Key Strengths:**
- Multi-target architecture enables gradual modernization
- Massive code reuse (~90%+) via Typewriter.Core minimizes risk
- .NET 8 CLI is cross-platform ready from day one
- Clear provider pattern boundary enables clean separation
- Existing test infrastructure reusable
- Official Microsoft dependencies improve PR acceptance
- DRY compliance via shared library extraction

**Areas for Future Enhancement:**
- Watch mode architecture (simpler with .NET 8)
- NuGet tool distribution (`dotnet tool install`)
- Performance profiling integration
- Eventual VS extension migration to newer .NET versions

### Implementation Handoff

**AI Agent Guidelines:**
- Follow all architectural decisions exactly as documented
- Use implementation patterns consistently across all components
- Respect project structure and boundaries (Core vs CLI vs VS)
- Extract shared code to Core before creating CLI-specific implementations
- Use `ConsoleOutput` for all user-facing messages (CLI only)
- Follow compiler-style error format
- Maintain netstandard2.0 compatibility for all Core code

**First Implementation Priority - Phase 1 (Typewriter.Core):**
1. Create `src/Core/Typewriter.Core.csproj` targeting netstandard2.0
2. Extract code model interfaces from `Typewriter.CodeModel`
3. Extract metadata interfaces from `Typewriter.Metadata`
4. Extract template engine (Parser, Compiler, TemplateCodeParser) from `Typewriter`
5. Update existing projects to reference `Typewriter.Core`
6. Add Core tests in `src/Tests/Core/`

**Second Implementation Priority - Phase 2 (Typewriter.CLI):**
1. Create `src/CLI/Typewriter.CLI.csproj` targeting net8.0
2. Reference `Typewriter.Core`
3. Implement `ConsoleOutput` utility class
4. Implement `CliMetadataProvider` and `CliRoslynWorkspace`
5. Implement CLI-specific `*Impl.cs` classes
6. Wire up `GenerateCommand` with System.CommandLine
7. Add CLI tests in `src/Tests/CLI/`

## Implementation Progress

### Completed Work

**Phase 0 - Foundation Retargeting (COMPLETED 2026-01-11):**

| Task | Status | Notes |
|------|--------|-------|
| Retarget `Typewriter.CodeModel` to netstandard2.0 | **DONE** | SDK-style project, builds successfully |
| Retarget `Typewriter.Metadata` to netstandard2.0 | **DONE** | SDK-style project, references CodeModel |
| Verify `Typewriter.Metadata.Roslyn` builds | **DONE** | net472, references netstandard2.0 assemblies |
| Verify `Typewriter` VS extension builds | **DONE** | net472, VSIX package produced |
| Verify solution builds end-to-end | **DONE** | All projects compile successfully |

**Project File Changes:**
- `src/CodeModel/Typewriter.CodeModel.csproj` - Converted to SDK-style, targets `netstandard2.0`
- `src/Metadata/Typewriter.Metadata.csproj` - Converted to SDK-style, targets `netstandard2.0`

Both projects use `SharedAssemblyInfo.cs` for version consistency and generate XML documentation.

### Remaining Work

**Phase 1 - Typewriter.Core (NOT STARTED):**
- Create `src/Core/Typewriter.Core.csproj` targeting netstandard2.0
- Extract generation engine (Parser, Compiler, TemplateCodeParser, ItemFilter)
- Create abstraction interfaces (IPathResolver, IErrorReporter)
- Update VS extension to reference Core

**Phase 2 - Typewriter.CLI (NOT STARTED):**
- Create `src/CLI/Typewriter.CLI.csproj` targeting net8.0
- Implement CLI infrastructure
- Implement CliMetadataProvider using Buildalyzer
- Wire up GenerateCommand with System.CommandLine

## Known Issues

### Existing Test Suite Failures

**Issue:** The existing test suite (`Typewriter.Tests`) fails when run outside of Visual Studio.

**Error:**
```
System.Runtime.InteropServices.COMException : SolutionDirectory must be called on the UI thread.
   at Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread(String callerMemberName)
   at Typewriter.Tests.TestInfrastructure.TestBase.get_SolutionDirectory()
```

**Root Cause:** The tests require VS SDK mocked infrastructure (`MefHostingFixture`, `DTE`, `ThreadHelper`) that needs to run in a VS experimental instance or with special test host configuration. This is **NOT related to the netstandard2.0 retargeting** - it's a pre-existing architectural constraint of the test suite.

**Impact:** 199 of 203 tests fail with UI thread errors when run via `vstest.console.exe` or `dotnet test`.

**Recommendation for Developers:**
- Do not spend time trying to fix these test failures - they are infrastructure-related, not code defects
- Tests that need VS infrastructure should be run within Visual Studio's test runner
- New CLI tests should be written to be VS-independent (no `MefHostingFixture`, no `DTE` dependencies)
- Consider creating a separate test category for VS-dependent vs VS-independent tests in the future

### Projects That Cannot Be Retargeted

The following projects have been analyzed and **cannot** be retargeted to netstandard2.0:

| Project | Reason |
|---------|--------|
| `Typewriter.Metadata.Roslyn` | Hard dependencies on VS SDK (`ThreadHelper`, `ServiceProvider`, `VisualStudioWorkspace`) |
| `Typewriter.ItemTemplates` | VS-specific VSIX component, not a portable library |
| `Typewriter` (VS Extension) | VS extension must target net472 for VS compatibility |

## Architecture Completion Summary

### Workflow Completion

**Architecture Decision Workflow:** COMPLETED (Updated for Multi-Target)
**Total Steps Completed:** 8
**Date Completed:** 2026-01-10
**Last Updated:** 2026-01-11 (Multi-target architecture revision)
**Document Location:** `_bmad-output/planning-artifacts/architecture.md`

### Final Architecture Deliverables

**Complete Architecture Document**
- Multi-target framework strategy documented (.NET 4.7.2 + netstandard2.0 + .NET 8)
- All architectural decisions documented with specific versions
- Implementation patterns ensuring AI agent consistency
- Complete project structure for both Core and CLI projects
- Code extraction strategy for DRY compliance
- Requirements to architecture mapping
- Validation confirming coherence and completeness

**Implementation Ready Foundation**
- 2 new projects defined (Typewriter.Core + Typewriter.CLI)
- 7 core architectural decisions made (including framework strategy)
- 11 implementation patterns defined (6 inherited + 5 CLI-specific)
- Code extraction strategy for ~1300 lines of shared code
- ~920 lines of new CLI-specific code
- 39 functional requirements fully supported

**AI Agent Implementation Guide**
- Multi-target technology stack with verified versions
- Consistency rules that prevent implementation conflicts
- Project structure with clear boundaries (Core vs CLI vs VS)
- Integration patterns and communication standards
- DRY compliance guidelines

### Development Sequence

**Phase 1 - Typewriter.Core (netstandard2.0):**
1. Create `src/Core/Typewriter.Core.csproj` targeting netstandard2.0
2. Extract interfaces from `Typewriter.CodeModel` and `Typewriter.Metadata`
3. Extract template engine from `Typewriter/Generation/`
4. Update existing VS extension to reference Core
5. Verify all existing tests pass with new structure
6. Add Core-specific tests in `src/Tests/Core/`

**Phase 2 - Typewriter.CLI (.NET 8):**
1. Create `src/CLI/Typewriter.CLI.csproj` targeting net8.0
2. Reference `Typewriter.Core`
3. Implement CLI infrastructure (`ConsoleOutput`, `CliRoslynWorkspace`)
4. Implement `CliMetadataProvider` using Buildalyzer + AdhocWorkspace
5. Implement CLI-specific `*Impl.cs` classes
6. Build command structure with System.CommandLine
7. Add CLI tests in `src/Tests/CLI/`
8. Verify output parity with VS extension

### Quality Assurance Checklist

**Architecture Coherence**
- [x] All decisions work together without conflicts
- [x] Multi-target technology choices are compatible (netstandard2.0 bridges .NET 4.7.2 and .NET 8)
- [x] Patterns support the architectural decisions
- [x] Structure aligns with existing solution
- [x] DRY principle enforced via shared Core library

**Requirements Coverage**
- [x] All functional requirements are supported
- [x] All non-functional requirements are addressed
- [x] Cross-cutting concerns are handled (7 concerns)
- [x] Integration points are defined
- [x] Future-proofing achieved via modern CLI framework

**Implementation Readiness**
- [x] Decisions are specific and actionable
- [x] Patterns prevent agent conflicts
- [x] Structure is complete and unambiguous
- [x] Examples are provided for clarity
- [x] Code extraction strategy is clear
- [x] Two-phase development sequence defined

---

**Architecture Status:** IMPLEMENTATION IN PROGRESS

**Completed:** Phase 0 (Foundation Retargeting) - CodeModel and Metadata now target netstandard2.0

**Next Phase:** Phase 1 (Typewriter.Core creation and generation engine extraction)

**Document Maintenance:** Update this architecture when major technical decisions are made during implementation.

---

## Revision History

| Date | Change | Author |
|------|--------|--------|
| 2026-01-10 | Initial architecture document | Noah + Winston |
| 2026-01-11 | Updated for multi-target architecture (.NET 8 CLI + netstandard2.0 Core) | Noah + Winston |
| 2026-01-11 | Completed Phase 0: Retargeted CodeModel and Metadata to netstandard2.0 | Noah |
| 2026-01-11 | Added Implementation Progress and Known Issues sections | Noah + Winston |

