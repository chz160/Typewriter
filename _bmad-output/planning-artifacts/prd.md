---
stepsCompleted: [1, 2, 3, 4, 7, 8, 9, 10, 11]
status: complete
inputDocuments:
  - docs/project-documentation/index.md
  - docs/project-documentation/project-overview.md
  - docs/project-documentation/architecture.md
  - docs/project-documentation/source-tree-analysis.md
  - docs/project-documentation/development-guide.md
workflowType: 'prd'
lastStep: 2
documentCounts:
  briefs: 0
  research: 0
  projectDocs: 5
  brainstorming: 0
---

# Product Requirements Document - Typewriter CLI Extension

**Author:** Noah
**Date:** 2026-01-10

## Executive Summary

Typewriter CLI extends the existing Typewriter Visual Studio extension to provide command-line TypeScript generation from C# code. This enables developers using VS Code, JetBrains Rider, or any text editor to leverage Typewriter's powerful template-based code generation without requiring Visual Studio.

The primary use case is enabling VS Code developers to execute `typewriter generate` directly from their terminal, eliminating the need to switch to Visual Studio solely for TypeScript generation. This unlocks Typewriter for teams and workflows that don't center on Visual Studio while preserving full compatibility with the existing VS extension.

### What Makes This Special

**Zero Context Switching:** Developers can stay in their preferred IDE and generate TypeScript with a single terminal command. No Visual Studio installation required for generation-only workflows.

**Maximum Code Reuse:** The existing Typewriter architecture follows clean provider patterns with ~60% VS-independent code. The CLI reuses the proven template engine, code model, and Roslyn analysis - only the VS-specific orchestration layer is replaced.

**Future-Proof Design:** By avoiding tight coupling to any specific build system naming or technology, the CLI architecture remains portable for eventual migration of the legacy portions of the solution beyond .NET Framework 4.7.2.

**Flexible Configuration:** Supports both CLI arguments for scripting flexibility and configuration files (`.typewriterrc`) for team-standardized workflows.

## Project Classification

| Attribute | Value |
|-----------|-------|
| **Technical Type** | CLI Tool + Developer Tool Extension |
| **Domain** | Developer Tooling / Code Generation |
| **Complexity** | Low-Medium |
| **Project Context** | Brownfield - extending existing VS extension |
| **Target Framework** | .NET 8 & .Net Standard 2.0 |
| **Primary Audience** | VS Code developers, CI/CD pipelines, non-VS workflows |

### Architecture Approach

The CLI will be implemented as a new `Typewriter.CLI` project that:
- References existing `Typewriter.CodeModel`, `Typewriter.Metadata`, and core generation components
- A .NET Standard 2.0 project `Typewriter.Core` should exist so that code from the legacy portion of the application can be refactored out and shared between the VS and CLI version of the tool.
- Introduces a standalone workspace provider (replacing `VisualStudioWorkspace`)
- Provides CLI argument parsing and configuration file support
- Outputs to console with configurable verbosity levels

This approach follows DRY and SOLID principles by reusing the maximum amount of existing code while changing the minimum amount of the current implementation.

## Success Criteria

### User Success

| Criteria | Measurement |
|----------|-------------|
| **Generate without Visual Studio** | VS Code developers can produce TypeScript from C# without launching Visual Studio |
| **Single command execution** | Running `typewriter generate` produces TypeScript files with zero mandatory configuration |
| **Output parity** | 100% identical TypeScript output compared to VS extension for same templates and sources |
| **Familiar workflow** | Existing `.tst` templates work unchanged - no template modifications required |

**The "aha!" moment:** A developer runs `typewriter generate` from their VS Code terminal and sees their TypeScript files appear instantly - same quality, no context switching.

### Business Success

| Criteria | Measurement |
|----------|-------------|
| **Team adoption** | VS Code developers on the team use CLI daily instead of switching to Visual Studio |
| **It just works** | CLI is reliable enough for regular use without issues |
| **Community value** | Contribution moves Typewriter into modern development workflows |

### Technical Success

| Criteria | Measurement |
|----------|-------------|
| **Maximum code reuse** | Reuses existing `Typewriter.CodeModel`, `Typewriter.Metadata`, and generation engine |
| **Minimal existing changes** | Zero or near-zero modifications to existing VS extension codebase |
| **Output parity** | Byte-for-byte identical TypeScript generation as VS extension |
| **Framework compatibility** | Legacy parts of the solution should remain .NET Framework 4.7.2, but the new CLI shoudl be .NET 8 |
| **Solution integration** | CLI project builds as part of existing `Typewriter.sln` |

### Measurable Outcomes

1. **Functional parity**: Given identical inputs (C# files + .tst templates), CLI produces identical outputs to VS extension
2. **Code reuse target**: >90% of generation logic reused from existing codebase
3. **Change footprint**: <100 lines modified in existing projects (excluding new CLI project)
4. **Team validation**: At least one full development cycle completed using CLI instead of VS extension

## Product Scope

### MVP - Minimum Viable Product

Core functionality required for useful daily operation:

- [ ] `typewriter generate` command that processes templates and outputs TypeScript
- [ ] Solution or project path specification via CLI argument
- [ ] Automatic template discovery (find all `.tst` files in solution)
- [ ] Basic console output (success/error messages, files generated)
- [ ] Standalone workspace provider (replacing VS-specific workspace)
- [ ] Exit codes for scripting integration (0 = success, non-zero = failure)

### Growth Features (Post-MVP)

Enhancements for broader adoption and usability:

- [ ] Configuration file support (`.typewriterrc` or `typewriter.json`)
- [ ] Configurable verbosity levels (`--quiet`, `--verbose`, `--debug`)
- [ ] Template filtering options (`--template`, `--include`, `--exclude`)
- [ ] Dry-run mode (`--dry-run` to preview without writing files)
- [ ] Output path override (`--output`)

### Vision (Future)

Long-term possibilities beyond initial release:

- [ ] NuGet package distribution for easy installation
- [ ] Cross-platform support (when migrating to .NET Core/.NET 8+)
- [ ] Watch mode for automatic regeneration on file changes
- [ ] Integration with dotnet CLI (`dotnet typewriter generate`)
- [ ] VS Code extension for enhanced integration

## User Journeys

### Journey 1: Alex Chen - Staying in the Flow

Alex is a full-stack developer working on an e-commerce platform with a C# API backend and React TypeScript frontend. The team uses Typewriter templates to keep their TypeScript DTOs in sync with C# models. Every time Alex updates a model class, they have to minimize VS Code, open Visual Studio, wait for it to load the solution, save a file to trigger generation, then switch back to VS Code. It breaks their concentration and costs 2-3 minutes each time.

One morning, Alex notices a new `typewriter` command mentioned in the team Slack. They open their VS Code terminal and type `typewriter generate --solution ./MyProject.sln`. Within seconds, the console shows "Generated 12 TypeScript files" and Alex sees their updated interfaces appear in the file tree. No context switching. No waiting for Visual Studio to load.

The breakthrough moment comes during a code review when Alex refactors five model classes in rapid succession, regenerating TypeScript after each change without ever leaving VS Code. What used to be a 15-minute interruption-filled process now takes 90 seconds of focused work. Alex's PR comment: "Finally, Typewriter works the way I work."

**Journey Reveals:** Core generation command, solution path argument, clear success output, fast execution

---

### Journey 2: Alex Chen - When Generation Goes Wrong

Alex pulls the latest code from the team repository and runs `typewriter generate` as usual. This time, the console shows an error: "Template compilation failed: CustomerModel.tst(15,8): Cannot resolve type 'OrderStatus'". Alex hasn't touched that template, so this is unexpected.

Alex checks the error output, which points to line 15 of the template file. They open `CustomerModel.tst` and see it references an `OrderStatus` enum that was moved to a different namespace in the recent PR. The template's `using` statement needs updating.

After fixing the template, Alex runs `typewriter generate` again. This time: "Generated 12 TypeScript files (1 warning)". The warning indicates a C# file couldn't be found - a model class that was deleted but the template still references it. Alex updates the template filter to exclude the removed class.

Third run: "Generated 12 TypeScript files". Success. Alex makes a mental note that the CLI error messages were clear enough to diagnose the issue without digging through documentation. They commit the template fixes along with their feature work.

**Journey Reveals:** Clear error messages with file/line references, warning vs error distinction, actionable diagnostic output, graceful handling of missing references

---

### Journey 3: Jordan Martinez - One Config to Rule Them All

Jordan is the tech lead on a team of six developers. After Alex's enthusiastic Slack messages about Typewriter CLI, Jordan decides it's time to standardize the team's workflow. Currently, each developer runs the command with slightly different arguments, and new team members always ask "how do I run the Typewriter thing?"

Jordan creates a `.typewriterrc` file in the repository root. They specify the solution path relative to the repo root, the template locations, and set verbosity to show which files were generated. They commit this to the main branch with a README update explaining the new workflow: "Just run `typewriter generate` from the repo root."

The next sprint planning, Jordan notices something unexpected - the team's velocity has ticked up slightly. When Jordan asks about it, a junior developer mentions "I used to avoid touching the C# models because regenerating TypeScript was confusing. Now I just run the command and it works." The config file didn't just standardize the workflow - it lowered the barrier for the whole team to work across the full stack.

Three months later, when a new developer joins, their onboarding document has one line for Typewriter: "Run `typewriter generate` after changing C# models." No Visual Studio installation required for frontend-focused developers.

**Journey Reveals:** Configuration file support, relative path resolution, team-shareable settings, zero-argument execution when config exists, lowered barrier for new team members

---

### Journey Requirements Summary

| Capability Area | Requirements Revealed |
|-----------------|----------------------|
| **Core Execution** | `typewriter generate` command, solution/project path input, template discovery, TypeScript output |
| **Output & Feedback** | Success message with file count, clear error messages with file:line references, warning vs error distinction |
| **Error Handling** | Template compilation errors, missing type references, missing file handling, actionable diagnostics |
| **Configuration** | `.typewriterrc` config file support, relative path resolution, default settings, config file discovery |
| **Team Workflow** | Zero-argument execution with config, portable/committable configuration, consistent cross-developer experience |
| **Exit Behavior** | Success/failure exit codes for scripting, non-zero exit on errors |

## CLI Tool Specific Requirements

### Command Structure

**Primary Command:**
```
typewriter generate [options]
```

**Core Arguments:**
| Argument | Description | Required |
|----------|-------------|----------|
| `--solution <path>` | Path to .sln file | No (if config exists) |
| `--project <path>` | Path to .csproj file (alternative to solution) | No |
| `--config <path>` | Path to config file | No (auto-discovered) |

**Behavior Flags (Growth):**
| Flag | Description | Phase |
|------|-------------|-------|
| `--quiet` | Suppress non-error output | Growth |
| `--verbose` | Detailed output including file-by-file status | Growth |
| `--json` | Machine-parseable JSON output | Growth |
| `--dry-run` | Preview without writing files | Growth |

### Output Formats

**MVP - Plain Text:**
```
Typewriter CLI v1.0.0
Processing solution: ./MyProject.sln
Found 3 templates

Generated 12 TypeScript files:
  - Models/Customer.ts
  - Models/Order.ts
  ...

Completed in 1.2s
```

**Error Output:**
```
Error: Template compilation failed
  File: CustomerModel.tst
  Line: 15, Column: 8
  Message: Cannot resolve type 'OrderStatus'
```

**Growth - JSON Format (`--json`):**
```json
{
  "success": true,
  "filesGenerated": 12,
  "duration": "1.2s",
  "files": ["Models/Customer.ts", "Models/Order.ts", ...]
}
```

### Configuration Schema

**Config File:** `.typewriterrc` or `typewriter.json` (JSON format)

**Resolution Order (highest to lowest priority):**
1. CLI arguments
2. Config file in current directory
3. Config file in solution/project root
4. Built-in defaults

**MVP Schema:**
```json
{
  "solution": "./MyProject.sln",
  "templates": ["**/*.tst"],
  "verbosity": "normal"
}
```

**Growth Schema Additions:**
```json
{
  "solution": "./MyProject.sln",
  "templates": ["**/*.tst"],
  "exclude": ["**/obj/**", "**/bin/**"],
  "output": "./generated",
  "verbosity": "normal"
}
```

### Scripting Support

**Exit Codes:**
| Code | Meaning |
|------|---------|
| 0 | Success - all files generated |
| 1 | Error - generation failed (template errors, missing files) |
| 2 | Error - invalid arguments or configuration |

**CI/CD Integration:**
- Non-interactive by default (no prompts)
- All configuration via arguments or config file
- Deterministic output for reproducible builds
- Exit codes enable pipeline failure detection

### Shell Completion (Growth Phase)

**Deferred to Growth phase:**
- Bash completion script
- Zsh completion script
- PowerShell completion script

### Implementation Considerations

**Standalone Workspace Provider:**
- Replace `VisualStudioWorkspace` with standalone Roslyn workspace
- Load solution/project from file path
- No VS dependencies in CLI execution path

**Reuse Strategy:**
- Reference existing `Typewriter.CodeModel` and `Typewriter.Metadata`
- Reuse `Parser`, `TemplateCodeParser`, `Compiler` from generation engine
- Create thin CLI wrapper around existing generation logic

**Error Handling:**
- Surface template compilation errors with file:line:column
- Surface C# analysis errors with actionable messages
- Graceful handling of missing files (warn, don't fail)

## Project Scoping & Phased Development

### MVP Strategy & Philosophy

**MVP Approach:** Problem-Solving MVP
- Solve the core problem (TypeScript generation without Visual Studio) with minimal features
- Fastest path to validated, usable tool
- Validates the CLI extraction approach before expanding functionality

**Project Size:** Simple MVP
- Small team (1-2 developers)
- Lean scope with clear boundaries
- ~60% code reuse from existing codebase reduces risk and effort

### MVP Feature Set (Phase 1)

**Core User Journeys Supported:**
- Journey 1: Alex generating TypeScript from VS Code terminal (happy path)
- Journey 2: Alex troubleshooting template errors (error handling)

**Must-Have Capabilities:**

| Feature | Without It... |
|---------|---------------|
| `typewriter generate` command | Product doesn't exist |
| `--solution` / `--project` arguments | Can't specify what to process |
| Automatic template discovery | Manual template paths too cumbersome |
| Plain text output (success/error) | No feedback on what happened |
| Standalone workspace provider | Still dependent on Visual Studio |
| Exit codes (0, 1, 2) | Can't use in scripts or detect failures |

**Explicitly Excluded from MVP:**
- Config file support (manual args are acceptable initially)
- JSON output format
- Verbose/quiet modes
- Dry-run preview
- Shell completion

### Post-MVP Features

**Phase 2: Growth**

| Priority | Feature | Enables |
|----------|---------|---------|
| 1 | Config file (`.typewriterrc`) | Journey 3: Team standardization |
| 2 | `--verbose` flag | Better debugging |
| 3 | `--quiet` flag | Cleaner script output |
| 4 | `--json` flag | CI/CD tooling integration |
| 5 | `--dry-run` flag | Safe preview before writing |
| 6 | Template filtering | Selective generation |

**Phase 3: Vision**

| Feature | Value |
|---------|-------|
| NuGet package distribution | `dotnet tool install typewriter` |
| Cross-platform support | When migrating to .NET 8+ |
| Watch mode | Continuous regeneration on save |
| `dotnet typewriter` integration | Native dotnet CLI experience |
| VS Code extension | Deep IDE integration with commands |

### Risk Mitigation Strategy

**Technical Risks:**

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Workspace provider behavior differs from VS | Medium | Comprehensive test suite comparing CLI vs VS output |
| Template compilation edge cases | Low | Reusing existing proven compiler code |
| Roslyn API changes | Low | Pin to same Roslyn version as VS extension |

**Market Risks:**

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Low adoption | Low | Solving clear pain point (context switching) |
| Existing alternatives | Low | None that reuse existing .tst templates |

**Resource Risks:**

| Risk | Mitigation |
|------|------------|
| Limited development time | MVP scope is intentionally minimal |
| Fewer resources than planned | MVP can ship with just core command |
| Technical blockers | Fallback: focus on happy path only |

### Decision Log

| Decision | Rationale |
|----------|-----------|
| MVP excludes config file | CLI args are sufficient for initial validation |
| Plain text output only in MVP | JSON/quiet modes are Growth enhancements |
| No watch mode | User explicitly scoped this out |

## Functional Requirements

### Code Generation

- FR1: User can generate TypeScript files from C# source files using .tst templates
- FR2: User can generate TypeScript for all templates in a solution with a single command
- FR3: User can generate TypeScript for a specific project instead of entire solution
- FR4: System produces identical TypeScript output as the Visual Studio extension given same inputs
- FR5: System processes all .tst template files found in the specified solution/project
- FR6: System executes custom C# code blocks embedded in templates (${...} syntax)
- FR7: System resolves template references (#reference directives) relative to template location

### Project Discovery

- FR8: User can specify a solution file path to process
- FR9: User can specify a project file path as an alternative to solution
- FR10: System automatically discovers all .tst template files within the solution/project
- FR11: System loads C# source files referenced by templates for code model extraction
- FR12: System resolves project references to include referenced project types in code model

### Output & Diagnostics

- FR13: User can see a summary of generated files after successful execution
- FR14: User can see which template files were processed
- FR15: User can see template compilation errors with file path, line number, and column
- FR16: User can see C# analysis errors with actionable messages
- FR17: System distinguishes between errors (blocking) and warnings (non-blocking)
- FR18: User can see total execution time after completion
- FR19: System outputs version information on startup

### Configuration (MVP)

- FR20: User can specify solution path via `--solution` command line argument
- FR21: User can specify project path via `--project` command line argument
- FR22: User can specify config file path via `--config` command line argument
- FR23: User can request help information via `--help` argument
- FR24: User can request version information via `--version` argument

### Configuration (Growth Phase)

- FR25: User can create a `.typewriterrc` or `typewriter.json` config file for default settings
- FR26: System discovers config file in current directory or solution root automatically
- FR27: User can set verbosity level in config file
- FR28: User can specify template include/exclude patterns in config file
- FR29: CLI arguments override config file settings when both are provided
- FR30: User can run `typewriter generate` with zero arguments when config file exists

### Output Modes (Growth Phase)

- FR31: User can suppress non-error output via `--quiet` flag
- FR32: User can enable detailed file-by-file output via `--verbose` flag
- FR33: User can request JSON-formatted output via `--json` flag
- FR34: User can preview generation without writing files via `--dry-run` flag

### Scripting Integration

- FR35: System exits with code 0 on successful generation
- FR36: System exits with code 1 on generation failure (template errors, missing files)
- FR37: System exits with code 2 on invalid arguments or configuration
- FR38: System operates non-interactively (no prompts or confirmations required)
- FR39: System writes errors to stderr and normal output to stdout

## Non-Functional Requirements

### Performance

| ID | Requirement | Metric |
|----|-------------|--------|
| NFR-P1 | CLI startup time shall be acceptable for interactive use | < 3 seconds cold start |
| NFR-P2 | Per-file generation time shall match VS extension performance | < 500ms per C# file |
| NFR-P3 | Solution loading shall complete in reasonable time for typical projects | < 30 seconds for 100-project solution |
| NFR-P4 | Memory usage shall remain bounded during generation | < 2GB for typical solutions |

### Reliability

| ID | Requirement | Metric |
|----|-------------|--------|
| NFR-R1 | Generation output shall be deterministic | Same inputs produce byte-identical outputs |
| NFR-R2 | CLI shall handle malformed input gracefully | No unhandled exceptions, clear error messages |
| NFR-R3 | Partial failures shall not corrupt previously generated files | Atomic file writes or rollback |
| NFR-R4 | Exit codes shall accurately reflect execution status | Documented exit code semantics |

### Maintainability

| ID | Requirement | Metric |
|----|-------------|--------|
| NFR-M1 | CLI shall share maximum code with VS extension | ≥ 60% shared codebase |
| NFR-M2 | CLI-specific code shall be isolated in dedicated project | Clean project boundaries |
| NFR-M3 | Provider pattern shall enable workspace swapping without template engine changes | No template engine modifications |
| NFR-M4 | Existing VS extension tests shall continue passing | 100% existing test pass rate |

### Compatibility

| ID | Requirement | Metric |
|----|-------------|--------|
| NFR-C1 | CLI shall run on Windows 10/11 with .NET 8 runetime | Verified on Windows 10 21H2+ |
| NFR-C2 | CLI shall produce identical output to VS extension for same inputs | Diff-verified equivalence |
| NFR-C3 | Existing .tst templates shall work without modification | 100% template compatibility |
| NFR-C4 | CLI shall work with solutions created by VS 2019/2022/2026 | MSBuild format compatibility |

### Usability

| ID | Requirement | Metric |
|----|-------------|--------|
| NFR-U1 | Error messages shall identify the source file and location | File path + line number when applicable |
| NFR-U2 | Help output shall be self-documenting | --help covers all options |
| NFR-U3 | Common operations shall require minimal arguments | Single command for typical use |

