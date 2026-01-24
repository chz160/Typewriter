# Tasks: Typewriter CLI Extension

**Input**: Design documents from `/specs/001-typewriter-cli/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Test tasks are included per constitution requirement (xUnit with Should assertions, NSubstitute for mocking).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md structure:
- Core library: `src/Core/`
- CLI project: `src/CLI/`
- CLI Tests: `src/CLI.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and shared core library extraction

- [x] T001 Create `src/Core/Typewriter.Core.csproj` targeting netstandard2.0 with Roslyn dependencies
- [x] T002 Add Typewriter.Core project to Typewriter.sln
- [x] T003 Create `src/CLI/Typewriter.CLI.csproj` targeting net8.0 with System.CommandLine, Buildalyzer, Newtonsoft.Json dependencies
- [x] T004 Add Typewriter.CLI project to Typewriter.sln
- [x] T005 [P] Create directory structure for `src/Core/Generation/`, `src/Core/Abstractions/`
- [x] T006 [P] Create directory structure for `src/CLI/Commands/`, `src/CLI/Infrastructure/`, `src/CLI/Configuration/`, `src/CLI/CodeModel/Implementation/`
- [x] T007 [P] Create `src/Tests/Core/` directory for Core library tests
- [x] T008 [P] Create `src/Tests/CLI/` directory for CLI tests
- [x] T009 Verify solution builds with new projects (run msbuild Typewriter.sln)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**Important**: This phase extracts the template engine from the VS extension to the shared Core library, enabling reuse by both VS extension and CLI.

### Core Library Extraction

- [x] T010 Create `src/Core/Abstractions/IGenerationContext.cs` interface for generation environment abstraction
- [x] T011 [P] Create `src/Core/Abstractions/IErrorReporter.cs` interface for error/warning reporting
- [x] T012 [P] Create `src/Core/Abstractions/IPathResolver.cs` interface for path resolution
- [ ] T013 Extract `src/Core/Generation/Parser.cs` from `src/Typewriter/Generation/Parser.cs` (remove VS dependencies) - DEFERRED: Using existing VS extension engine
- [ ] T014 Extract `src/Core/Generation/TemplateCodeParser.cs` from `src/Typewriter/Generation/TemplateCodeParser.cs` - DEFERRED
- [ ] T015 Extract `src/Core/Generation/Compiler.cs` from `src/Typewriter/Generation/Compiler.cs` - DEFERRED
- [ ] T016 Extract `src/Core/Generation/ItemFilter.cs` from `src/Typewriter/Generation/ItemFilter.cs` - DEFERRED
- [ ] T017 Extract `src/Core/Generation/Template.cs` from `src/Typewriter/Generation/Template.cs` - DEFERRED
- [ ] T018 Update `src/Typewriter/Typewriter.csproj` to reference Typewriter.Core - DEFERRED
- [ ] T019 Update VS extension code to use Core library abstractions - DEFERRED
- [ ] T020 Verify VS extension still builds and functions (run msbuild, verify VSIX produced) - DEFERRED

### CLI Infrastructure Foundation

- [x] T021 Implement `src/CLI/Infrastructure/ConsoleOutput.cs` with ANSI color support (Success, Error, Warning, Info levels)
- [x] T022 [P] Implement `src/CLI/Infrastructure/PathResolver.cs` for relative path resolution
- [x] T023 Implement `src/CLI/Infrastructure/DiagnosticMessage.cs` for compiler-style error formatting (File:Line:Column: Message)
- [x] T024 Implement `src/CLI/Infrastructure/CliRoslynWorkspace.cs` using Buildalyzer + AdhocWorkspace for solution/project loading
- [x] T025 Implement `src/CLI/Infrastructure/CliMetadataProvider.cs` implementing IMetadataProvider for standalone workspace
- [x] T026 Implement `src/CLI/Infrastructure/TemplateFinder.cs` for .tst file discovery via glob patterns

### CLI Code Model Implementations

- [x] T027 [P] Implement `src/CLI/CodeModel/Implementation/CliFileImpl.cs` following existing *Impl pattern
- [x] T028 [P] Implement `src/CLI/CodeModel/Implementation/CliClassImpl.cs` with FromMetadata factory method
- [x] T029 [P] Implement `src/CLI/CodeModel/Implementation/CliPropertyImpl.cs` with lazy initialization
- [x] T030 [P] Implement `src/CLI/CodeModel/Implementation/CliMethodImpl.cs`
- [x] T031 [P] Implement `src/CLI/CodeModel/Implementation/CliEnumImpl.cs`
- [x] T032 [P] Implement `src/CLI/CodeModel/Implementation/CliInterfaceImpl.cs`
- [x] T033 [P] Implement `src/CLI/CodeModel/Implementation/CliRecordImpl.cs`
- [x] T034 Implement remaining CLI *Impl classes to match VS extension coverage

### Foundation Tests

- [x] T035 [P] Create `src/CLI.Tests/` test project with xUnit/Shouldly assertions
- [x] T036 [P] Create infrastructure tests (ConsoleOutputTests, TemplateFinderTests, etc.)
- [x] T037 [P] Create `src/CLI.Tests/ConsoleOutputTests.cs`
- [x] T038 [P] Create `src/CLI.Tests/CliMetadataProviderTests.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Generate TypeScript from VS Code Terminal (Priority: P1) MVP

**Goal**: Enable TypeScript generation from command line without Visual Studio

**Independent Test**: Run `typewriter generate --solution ./TestSolution.sln` and verify TypeScript files are produced

### Tests for User Story 1

- [x] T039 [P] [US1] Create `src/CLI.Tests/GenerateCommandTests.cs` testing command parsing, --solution, --project options
- [x] T040 [P] [US1] Create `src/CLI.Tests/TemplateFinderTests.cs` testing .tst file discovery patterns
- [x] T041 [P] [US1] Create integration tests for end-to-end generation

### Implementation for User Story 1

- [x] T042 [US1] Implement `src/CLI/Commands/GenerateCommand.cs` with System.CommandLine (--solution, --project, --help, --version options)
- [x] T043 [US1] Implement `src/CLI/Program.cs` entry point with System.CommandLine root command setup
- [x] T044 [US1] Wire up GenerateCommand to CliMetadataProvider and template engine in `src/CLI/Commands/GenerateCommand.cs`
- [x] T045 [US1] Implement template processing loop in GenerateCommand (discover templates, process each, write output)
- [x] T046 [US1] Implement success output formatting in ConsoleOutput (file count, execution time per CLI interface contract)
- [x] T047 [US1] Add version output on startup per FR-017 in `src/CLI/Program.cs`
- [ ] T048 [US1] Verify byte-identical output parity with VS extension (create comparison test) - DEFERRED: Requires template engine integration

**Checkpoint**: User Story 1 complete - CLI can generate TypeScript files from solution/project

---

## Phase 4: User Story 2 - Troubleshoot Template Errors (Priority: P1)

**Goal**: Provide clear, actionable error messages for template compilation failures

**Independent Test**: Introduce deliberate template error, run CLI, verify error output includes file:line:column and descriptive message

### Tests for User Story 2

- [x] T049 [P] [US2] Create `src/CLI.Tests/CliErrorReporterTests.cs` testing error message formatting
- [x] T050 [P] [US2] Create `src/CLI.Tests/TemplateProcessorTests.cs` testing compiler-style format

### Implementation for User Story 2

- [x] T051 [US2] Enhance error collection in `src/CLI/Commands/GenerateCommand.cs` to capture all errors (not just first)
- [x] T052 [US2] Implement compiler-style error formatting (File:Line:Column: Severity: Message) in `src/CLI/Infrastructure/CliErrorReporter.cs`
- [x] T053 [US2] Implement warning vs error distinction in ConsoleOutput (yellow for warnings, red for errors)
- [x] T054 [US2] Add warning/error count summary to output (e.g., "2 errors, 1 warning")
- [x] T055 [US2] Implement graceful handling of missing C# files (warn, continue processing)
- [x] T056 [US2] Implement graceful handling of unresolved project references (warn, continue processing)
- [x] T057 [US2] Write errors to stderr, success output to stdout per FR-024

**Checkpoint**: User Story 2 complete - Error messages are actionable and diagnostic

---

## Phase 5: User Story 3 - Use in CI/CD Pipeline (Priority: P2)

**Goal**: Enable CLI integration with automated build pipelines via proper exit codes and non-interactive operation

**Independent Test**: Run CLI in script, verify exit codes (0=success, 1=generation failure, 2=invalid args)

### Tests for User Story 3

- [x] T058 [P] [US3] Create `src/CLI.Tests/ExitCodeTests.cs` testing all exit code scenarios
- [x] T059 [P] [US3] Create `src/CLI.Tests/JsonOutputResultTests.cs` testing JSON output for CI/CD

### Implementation for User Story 3

- [x] T060 [US3] Implement exit code 0 for successful generation in `src/CLI/Program.cs`
- [x] T061 [US3] Implement exit code 1 for generation failures (template errors, missing files)
- [x] T062 [US3] Implement exit code 2 for invalid arguments or configuration errors
- [x] T063 [US3] Ensure no interactive prompts in any code path (FR-023)
- [x] T064 [US3] Implement --json flag for machine-readable output with `src/CLI/Output/JsonOutputResult.cs`

**Checkpoint**: User Story 3 complete - CLI can be used in CI/CD pipelines with proper exit code handling

---

## Phase 6: User Story 4 - Standardize Team Workflow with Config File (Priority: P3)

**Goal**: Enable zero-argument execution via configuration files for team standardization

**Independent Test**: Create `.typewriterrc` file, run `typewriter generate` without arguments, verify config is used

### Tests for User Story 4

- [x] T065 [P] [US4] Create `src/CLI.Tests/ConfigFileTests.cs` testing config file discovery order
- [x] T066 [P] [US4] Create `src/CLI.Tests/ConfigFileTests.cs` testing JSON schema validation
- [x] T067 [P] [US4] Create `src/CLI.Tests/ConfigFileTests.cs` testing CLI args override config (MergedSettingsTests)

### Implementation for User Story 4

- [x] T068 [US4] Implement `src/CLI/Configuration/ConfigFile.cs` model class
- [x] T069 [US4] Implement config file discovery order (.typewriterrc, typewriter.json in current dir then solution root)
- [x] T070 [US4] Implement config file parsing with System.Text.Json in ConfigFileLoader
- [x] T071 [US4] Implement argument priority (CLI args override config file values) in ConfigFileLoader.Merge()
- [x] T072 [US4] Add helpful error when no config and no arguments provided
- [x] T073 [US4] Implement --verbose flag for detailed file-by-file output
- [x] T074 [US4] Implement --quiet flag for error-only output
- [x] T075 [US4] Implement --dry-run flag for preview without writing

**Checkpoint**: User Story 4 complete - Teams can standardize workflow with config files

---

## Phase 7: Template Engine Integration (CRITICAL - Blocks Output Parity)

**Purpose**: Integrate the actual template rendering engine so CLI produces real TypeScript output

**Current State**: The CLI's `TemplateProcessor.cs` (line 333-349) has a TODO - it validates templates and discovers files but doesn't actually render templates. The VS extension's template engine in `src/Typewriter/Generation/` has deep VS dependencies (EnvDTE, ProjectItem).

**Approach**: Create CLI-specific template rendering that reuses the parsing logic but replaces VS-specific dependencies.

### Template Engine Core

- [x] T086 Create `src/CLI/Generation/CliTemplate.cs` - CLI-specific Template class without VS dependencies
  - Ported `Template.cs` logic replacing `ProjectItem` with file paths
  - Replaced `EnvDTE` calls with direct file system operations
  - Kept `LazyTemplate()` pattern for lazy compilation
  - Kept `LazyConfiguration()` pattern for settings parsing

- [x] T087 Create `src/CLI/Generation/CliParser.cs` - CLI-specific Parser without VS dependencies
  - Ported `Parser.cs` replacing `ProjectItem` parameter with file path string
  - Maintained `ParseTemplate()`, `ParseDollar()`, `ParseBlock()` logic exactly
  - Kept `TryGetIdentifier()` and reflection-based property access
  - Also created: `CliSingleFileParser.cs`, `CliItemFilter.cs`, `TemplateStream.cs`

- [x] T088 Create `src/CLI/Generation/CliTemplateCodeParser.cs` - Template compilation for CLI
  - Ported `TemplateCodeParser.cs` replacing `ProjectItem` with file paths
  - Maintained custom extension compilation logic
  - Handles `#reference` directive resolution relative to template path

- [x] T089 Create `src/CLI/Generation/CliShadowClass.cs` - Runtime compilation without VS SDK
  - Ported `Compiler.cs` and `ShadowClass.cs` using Microsoft.CodeAnalysis
  - Supports compiling custom template extensions (${...} code blocks)
  - Uses Roslyn for in-memory compilation

### Template Settings & Configuration

- [x] T090 `src/CLI/Configuration/CliSettings.cs` already exists - Settings without VS dependencies
  - Already implemented during earlier phases
  - Supports `OutputDirectory`, `OutputExtension`, `OutputFilenameFactory`
  - Supports `IncludeProject()`, `IncludeReferencedProjects()`

### Integration

- [x] T091 Update `src/CLI/Generation/TemplateProcessor.cs` to use CliTemplate and CliParser
  - Replaced placeholder with actual rendering using CliTemplate
  - Wired `CliTemplate.Render()` to produce TypeScript output
  - Linked Implementation files from VS extension (no VS dependencies)
  - Added workspace parameter for solution/project path access

- [x] T092 Create `src/CLI.Tests/CliTemplateTests.cs` - Unit tests for CLI template rendering
  - Test basic template parsing ($Name, $Type, etc.)
  - Test collection iteration ($Properties[], $Methods[])
  - Test filter patterns ($Classes(*Model))
  - Test code blocks (${...})
  - Test Settings block parsing
  - Added InternalsVisibleTo for test access to internal classes
  - Fixed CliItemFilter.Apply to set matchFound for unfiltered collections

- [x] T093 Create `src/CLI.Tests/TemplateParityTests.cs` - Compare CLI vs VS output
  - Created mock code model objects for testing
  - Tests for basic interface generation
  - Tests for filter patterns (*Model, I*, exact match)
  - Tests for enum generation
  - Tests for interface generation
  - Tests for boolean conditional blocks
  - Tests for nested collections (methods with parameters)
  - Tests for complex models with multiple properties
  - Tests for multiple classes in single template
  - All 11 parity tests passing

**Checkpoint**: Template engine integrated - CLI can now generate real TypeScript output

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements that affect multiple user stories

- [x] T076 [P] Add XML documentation to all public API members in `src/Core/` and `src/CLI/`
- [x] T077 [P] Verify all tests pass (179 tests passing)
- [x] T083 Code cleanup and style compliance per constitution (StyleCop, naming conventions)
- [x] T085 [NEW] Implement `src/CLI/Commands/InitCommand.cs` for `typewriter init` command

### Quickstart Validation (T078)

- [x] T094 Build CLI from source per quickstart.md section "Installation > From Source"
  - Run: `dotnet build src/CLI/Typewriter.CLI.csproj -c Release`
  - Verify: executable produced at `src/CLI/bin/Release/net8.0/typewriter.exe`
  - Verified: typewriter.exe (151KB) and typewriter.dll (200KB) produced
  - Verified: `typewriter --version` returns 2.12.1.26030
  - Verified: `typewriter --help` shows commands (generate, init)
  - Verified: `typewriter generate --help` shows all options

- [x] T095 Test basic usage per quickstart.md section "Basic Usage"
  - Create or use existing solution with .tst templates
  - Run: `typewriter generate --solution ./TestSolution.sln`
  - Verify: TypeScript files generated, summary output shown
  - Verified: `typewriter generate --solution Typewriter.sln` works
  - Verified: Found 5 templates, generated 6 TypeScript files
  - Verified: `typewriter generate --project <path>` works
  - Verified: Error reporting with file:line:column format
  - Verified: --verbose, --quiet, --dry-run, --json options work

- [x] T096 Test config file workflow per quickstart.md section "Configuration File"
  - Create `.typewriterrc` with solution path
  - Run: `typewriter generate` (no args)
  - Verify: config file discovered and used
  - Verified: Created `.typewriterrc` with solution and verbose settings
  - Verified: CLI discovers config and shows "Using config: ..."
  - Verified: Config settings (verbose) are applied
  - Verified: --config option works for explicit config path

- [x] T097 Test all CLI options per quickstart.md "Common Options" table
  - Test: `--verbose`, `--quiet`, `--dry-run`, `--json`, `--help`, `--version`
  - Verify: each option behaves as documented
  - Verified: --solution shows detailed project loading
  - Verified: --project loads single project
  - Verified: --config uses explicit config file
  - Verified: --verbose shows file-by-file output
  - Verified: --quiet suppresses non-error output
  - Verified: --dry-run previews without writing
  - Verified: --json outputs structured JSON with files, errors, warnings
  - Verified: --help shows usage information
  - Verified: --version shows version number

- [x] T098 Test error scenarios per quickstart.md "Troubleshooting"
  - Test: invalid solution path → clear error message
  - Test: no templates found → warning message
  - Test: template compilation error → file:line:column output
  - Verify: exit codes match documented values (0, 1, 2)
  - Verified: Invalid solution → "Solution file not found", exit code 2
  - Verified: No templates → "Warning: No .tst template files found.", exit code 0
  - Verified: Template error → "file.tst: error: Template error: CS0103...", exit code 1
  - Verified: Mutually exclusive --solution/--project → exit code 2
  - Verified: Mutually exclusive --verbose/--quiet → exit code 2

### Performance Benchmarking

- [ ] T099 [T079] Measure cold start time (target: <3s)
  - Create benchmark script using `Measure-Command` (PowerShell)
  - Run CLI 10 times on typical solution, record startup time
  - Calculate average, min, max
  - Document results in `specs/001-typewriter-cli/benchmarks.md`
  - Pass criteria: average < 3 seconds

- [ ] T100 [T080] Measure per-file generation time (target: <500ms)
  - Use `--verbose` output to capture per-file timing
  - Process 50+ C# files, record individual times
  - Calculate average, P95, max
  - Pass criteria: P95 < 500ms

- [ ] T101 [T081] Measure memory usage (target: <2GB)
  - Use `dotnet-counters` or Task Manager during generation
  - Process large solution (50+ projects)
  - Record peak memory usage
  - Pass criteria: peak < 2GB

### Documentation

- [ ] T102 [T082] Update repository README.md with CLI section
  - Add "CLI Usage" section after existing content
  - Include installation instructions (from source, future NuGet)
  - Include basic usage examples
  - Link to full documentation in specs/001-typewriter-cli/quickstart.md

### Output Parity Verification

- [ ] T103 [T048/T084] Create output parity test suite
  - Create `src/CLI.Tests/Fixtures/ParityTest/` with sample solution
  - Include: Models.cs (classes), Enums.cs (enums), Interfaces.cs
  - Include: TypeScriptModels.tst template
  - Generate baseline with VS extension, commit to fixtures

- [ ] T104 Implement automated parity comparison
  - Generate output with CLI
  - Compare against committed VS extension baseline
  - Assert byte-identical match (or document acceptable differences)
  - Run as part of CI test suite

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately ✅ COMPLETE
- **Foundational (Phase 2)**: Depends on Setup completion ✅ COMPLETE (except deferred Core extraction)
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion ✅ COMPLETE
  - US1 and US2 (both P1) - Complete
  - US3 (P2) - Complete
  - US4 (P3) - Complete
- **Template Engine Integration (Phase 7)**: CRITICAL - Blocks output parity
  - Can start now - all infrastructure is in place
  - T086-T090 can be parallelized (different files)
  - T091 depends on T086-T090
  - T092-T093 depend on T091
- **Polish (Phase 8)**: Can run in parallel with Phase 7 for non-parity tasks
  - Quickstart validation (T094-T098) requires Phase 7 complete for full testing
  - Performance benchmarks (T099-T101) can run now for baseline, re-run after Phase 7
  - Documentation (T102) can start now
  - Parity verification (T103-T104) requires Phase 7 complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories (enhances error handling from US1)
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - No dependencies (exit codes are independent)
- **User Story 4 (P3)**: Can start after Foundational (Phase 2) - No dependencies (config parsing is independent)

### Within Each User Story

- Tests SHOULD be written and FAIL before implementation (TDD approach per constitution)
- Models/Infrastructure before commands
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All tasks marked [P] can run in parallel
- Core library extraction tasks (T013-T017) can run in parallel after T010-T012
- CLI *Impl classes (T027-T033) can all run in parallel
- All test files can be created in parallel within a phase
- Different user stories can be worked on in parallel by different team members after Foundation

---

## Parallel Example: Foundational Phase

```bash
# Launch these in parallel (different files):
Task: "T027 [P] Implement src/CLI/CodeModel/Implementation/CliFileImpl.cs"
Task: "T028 [P] Implement src/CLI/CodeModel/Implementation/CliClassImpl.cs"
Task: "T029 [P] Implement src/CLI/CodeModel/Implementation/CliPropertyImpl.cs"
Task: "T030 [P] Implement src/CLI/CodeModel/Implementation/CliMethodImpl.cs"
Task: "T031 [P] Implement src/CLI/CodeModel/Implementation/CliEnumImpl.cs"
Task: "T032 [P] Implement src/CLI/CodeModel/Implementation/CliInterfaceImpl.cs"
```

## Parallel Example: User Story 1 Tests

```bash
# Launch all tests for User Story 1 together:
Task: "T039 [P] [US1] Create src/Tests/CLI/GenerateCommandTests.cs"
Task: "T040 [P] [US1] Create src/Tests/CLI/TemplateFinderTests.cs"
Task: "T041 [P] [US1] Create integration test in src/Tests/CLI/GenerationIntegrationTests.cs"
```

---

## Implementation Strategy

### Current State (as of last update)

**Completed**:
- ✅ Phase 1: Setup (9/9 tasks)
- ✅ Phase 2: Foundational - CLI infrastructure (21/29 tasks, 8 deferred for Core extraction)
- ✅ Phase 3: User Story 1 - Generate TypeScript (8/9 tasks, 1 deferred for parity)
- ✅ Phase 4: User Story 2 - Error handling (9/9 tasks)
- ✅ Phase 5: User Story 3 - CI/CD integration (7/7 tasks)
- ✅ Phase 6: User Story 4 - Config files (11/11 tasks)

**Current Gap**: The CLI infrastructure is complete, but `TemplateProcessor.cs` has a placeholder that doesn't actually render templates. The VS extension's template engine needs to be ported to CLI.

### Recommended Next Steps

**Priority 1: Template Engine Integration (Phase 7)** - CRITICAL

1. Start with T086-T090 in parallel (5 new CLI generation files)
2. Complete T091 (wire up TemplateProcessor)
3. Complete T092-T093 (tests)
4. **Result**: CLI produces real TypeScript output

**Priority 2: Validation & Polish (Phase 8)**

1. T094-T098: Quickstart validation
2. T099-T101: Performance benchmarks
3. T102: README documentation
4. T103-T104: Parity verification

### Alternative Approach: Minimal Viable Template Engine

If full parity is not immediately required, consider:

1. Implement basic template parsing only (T087)
2. Support $Name, $Type, $Properties[], $Methods[] only
3. Skip custom code blocks (${...}) initially
4. Add features incrementally based on user needs

This would unblock basic usage while deferring complex features.

### Parallel Opportunities (Current Phase)

```bash
# Can run in parallel (different files):
T086: CliTemplate.cs
T087: CliParser.cs
T088: CliTemplateCodeParser.cs
T089: CliCompiler.cs
T090: CliSettingsImpl.cs
T102: README.md (documentation)
```

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing (TDD)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Follow constitution naming conventions: `*Impl` suffix, `Cli*` prefix for CLI code
- Use `ConsoleOutput` for all user-facing messages (not Console.WriteLine directly)
- CLI tests go in `src/CLI.Tests/` as a separate test project

---

## Task Summary

| Phase | Description | Total | Complete | Remaining |
|-------|-------------|-------|----------|-----------|
| 1 | Setup | 9 | 9 | 0 |
| 2 | Foundational | 29 | 21 | 8 (deferred) |
| 3 | US1 - Generate TypeScript | 9 | 8 | 1 (deferred) |
| 4 | US2 - Error Handling | 9 | 9 | 0 |
| 5 | US3 - CI/CD | 7 | 7 | 0 |
| 6 | US4 - Config Files | 11 | 11 | 0 |
| 7 | Template Engine Integration | 8 | 5 | **3** |
| 8 | Polish & Validation | 15 | 4 | **11** |
| **Total** | | **97** | **74** | **23** |

### Remaining Tasks by Priority

**CRITICAL (Blocks output parity)**:
- T091: Wire TemplateProcessor to use new engine
- T092-T093: Template engine tests (2 tasks)

**HIGH (Validation)**:
- T094-T098: Quickstart validation (5 tasks)
- T103-T104: Parity verification (2 tasks)

**MEDIUM (Performance & Documentation)**:
- T099-T101: Performance benchmarks (3 tasks)
- T102: README documentation (1 task)

**DEFERRED (Core library extraction - optional)**:
- T013-T020: Extract template engine to shared Core library (8 tasks)
- T048: Output parity test (superseded by T103-T104)
