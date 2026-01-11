---
stepsCompleted: ['step-01-validate-prerequisites', 'step-02-design-epics', 'step-03-create-stories', 'step-04-final-validation']
status: complete
completedAt: '2026-01-10'
inputDocuments:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/architecture.md
---

# Typewriter CLI - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Typewriter CLI, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

**Code Generation (FR1-7):**
- FR1: User can generate TypeScript files from C# source files using .tst templates
- FR2: User can generate TypeScript for all templates in a solution with a single command
- FR3: User can generate TypeScript for a specific project instead of entire solution
- FR4: System produces identical TypeScript output as the Visual Studio extension given same inputs
- FR5: System processes all .tst template files found in the specified solution/project
- FR6: System executes custom C# code blocks embedded in templates (${...} syntax)
- FR7: System resolves template references (#reference directives) relative to template location

**Project Discovery (FR8-12):**
- FR8: User can specify a solution file path to process
- FR9: User can specify a project file path as an alternative to solution
- FR10: System automatically discovers all .tst template files within the solution/project
- FR11: System loads C# source files referenced by templates for code model extraction
- FR12: System resolves project references to include referenced project types in code model

**Output & Diagnostics (FR13-19):**
- FR13: User can see a summary of generated files after successful execution
- FR14: User can see which template files were processed
- FR15: User can see template compilation errors with file path, line number, and column
- FR16: User can see C# analysis errors with actionable messages
- FR17: System distinguishes between errors (blocking) and warnings (non-blocking)
- FR18: User can see total execution time after completion
- FR19: System outputs version information on startup

**Configuration - MVP (FR20-24):**
- FR20: User can specify solution path via `--solution` command line argument
- FR21: User can specify project path via `--project` command line argument
- FR22: User can specify config file path via `--config` command line argument
- FR23: User can request help information via `--help` argument
- FR24: User can request version information via `--version` argument

**Configuration - Growth Phase (FR25-30):**
- FR25: User can create a `.typewriterrc` or `typewriter.json` config file for default settings
- FR26: System discovers config file in current directory or solution root automatically
- FR27: User can set verbosity level in config file
- FR28: User can specify template include/exclude patterns in config file
- FR29: CLI arguments override config file settings when both are provided
- FR30: User can run `typewriter generate` with zero arguments when config file exists

**Output Modes - Growth Phase (FR31-34):**
- FR31: User can suppress non-error output via `--quiet` flag
- FR32: User can enable detailed file-by-file output via `--verbose` flag
- FR33: User can request JSON-formatted output via `--json` flag
- FR34: User can preview generation without writing files via `--dry-run` flag

**Scripting Integration (FR35-39):**
- FR35: System exits with code 0 on successful generation
- FR36: System exits with code 1 on generation failure (template errors, missing files)
- FR37: System exits with code 2 on invalid arguments or configuration
- FR38: System operates non-interactively (no prompts or confirmations required)
- FR39: System writes errors to stderr and normal output to stdout

### NonFunctional Requirements

**Performance:**
- NFR-P1: CLI startup time < 3 seconds cold start
- NFR-P2: Per-file generation time < 500ms per C# file
- NFR-P3: Solution loading < 30 seconds for 100-project solution
- NFR-P4: Memory usage < 2GB for typical solutions

**Reliability:**
- NFR-R1: Generation output shall be deterministic (byte-identical outputs)
- NFR-R2: CLI shall handle malformed input gracefully (no unhandled exceptions)
- NFR-R3: Partial failures shall not corrupt previously generated files (atomic writes)
- NFR-R4: Exit codes shall accurately reflect execution status

**Maintainability:**
- NFR-M1: CLI shall share ≥60% code with VS extension
- NFR-M2: CLI-specific code isolated in dedicated project
- NFR-M3: Provider pattern enables workspace swapping without template engine changes
- NFR-M4: Existing VS extension tests shall continue passing (100%)

**Compatibility:**
- NFR-C1: CLI runs on Windows 10/11 with .NET Framework 4.7.2
- NFR-C2: CLI produces identical output to VS extension (diff-verified)
- NFR-C3: Existing .tst templates work without modification
- NFR-C4: CLI works with VS 2019/2022/2025 solution formats

**Usability:**
- NFR-U1: Error messages include file path and line number
- NFR-U2: --help is self-documenting
- NFR-U3: Common operations require minimal arguments

### Additional Requirements

**From Architecture - Starter Template:**
- NOT applicable - this is a brownfield extension project
- New `Typewriter.CLI` project added to existing `Typewriter.sln`

**From Architecture - Technology Stack:**
- System.CommandLine for argument parsing (official Microsoft library)
- AdhocWorkspace + Buildalyzer for standalone workspace provider (Buildalyzer already in solution)
- Newtonsoft.Json for configuration file parsing
- ANSI colors for console output (zero dependencies)
- Compiler-style error format: `File:Line:Column: Message`

**From Architecture - Project Structure:**
- New project: `src/CLI/Typewriter.CLI.csproj`
- New files: Program.cs, GenerateCommand.cs, CliMetadataProvider.cs, ConsoleOutput.cs, TemplateFinder.cs, PathResolver.cs, CliSettings.cs
- Tests location: `src/Tests/CLI/` subfolder
- Estimated ~510 lines of new code, 90%+ code reuse

**From Architecture - Implementation Patterns:**
- `*Impl` suffix for implementation wrappers (inherited)
- `*Command` suffix for command classes (CLI-specific)
- `ConsoleOutput` static class for all user-facing messages
- Exit codes: 0=success, 1=generation failure, 2=invalid args
- Errors to stderr, normal output to stdout

**From Architecture - Constraints:**
- .NET Framework 4.7.2 (must match existing codebase)
- Same Roslyn version as VS extension (4.14.0)
- Zero VS dependencies in CLI execution path
- Generic naming (avoid build-system-specific names)

### FR Coverage Map

| FR | Epic | Description |
|----|------|-------------|
| FR1 | Epic 1 | Generate TypeScript from C# using .tst templates |
| FR2 | Epic 2 | Generate for all templates in solution |
| FR3 | Epic 2 | Generate for specific project only |
| FR4 | Epic 1 | Identical output to VS extension |
| FR5 | Epic 1 | Process all .tst template files |
| FR6 | Epic 1 | Execute custom C# code blocks (${...}) |
| FR7 | Epic 1 | Resolve template references (#reference) |
| FR8 | Epic 1 | Specify solution file path |
| FR9 | Epic 2 | Specify project file as alternative |
| FR10 | Epic 2 | Auto-discover .tst files |
| FR11 | Epic 2 | Load C# source files for code model |
| FR12 | Epic 2 | Resolve project references |
| FR13 | Epic 3 | Summary of generated files |
| FR14 | Epic 3 | Which templates were processed |
| FR15 | Epic 3 | Template errors with file:line:column |
| FR16 | Epic 3 | C# analysis errors with actionable messages |
| FR17 | Epic 3 | Error vs warning distinction |
| FR18 | Epic 3 | Total execution time |
| FR19 | Epic 3 | Version info on startup |
| FR20 | Epic 1 | `--solution` CLI argument |
| FR21 | Epic 2 | `--project` CLI argument |
| FR22 | Epic 4 | `--config` argument |
| FR23 | Epic 1 | `--help` argument |
| FR24 | Epic 1 | `--version` argument |
| FR25 | Epic 4 | `.typewriterrc` or `typewriter.json` support |
| FR26 | Epic 4 | Auto-discover config in cwd/solution root |
| FR27 | Epic 4 | Verbosity setting in config |
| FR28 | Epic 4 | Template include/exclude patterns |
| FR29 | Epic 4 | CLI args override config settings |
| FR30 | Epic 4 | Zero-argument execution with config |
| FR31 | Epic 5 | `--quiet` flag |
| FR32 | Epic 5 | `--verbose` flag |
| FR33 | Epic 5 | `--json` flag |
| FR34 | Epic 5 | `--dry-run` flag |
| FR35 | Epic 1 | Exit code 0 on success |
| FR36 | Epic 3 | Exit code 1 on generation failure |
| FR37 | Epic 3 | Exit code 2 on invalid args |
| FR38 | Epic 1 | Non-interactive operation |
| FR39 | Epic 3 | Errors to stderr, output to stdout |

## Epic List

### Epic 1: Generate Command Foundation (MVP)

Developer runs `typewriter generate --solution ./MyProject.sln` and TypeScript files appear. This is the core promise - TypeScript generation without Visual Studio.

**FRs covered:** FR1, FR4, FR5, FR6, FR7, FR8, FR20, FR23, FR24, FR35, FR38

**User Outcome:** Developer can generate TypeScript from C# without launching Visual Studio.

**Implementation Notes:**
- New `Typewriter.CLI` project with System.CommandLine
- `CliMetadataProvider` using AdhocWorkspace + Buildalyzer
- Reuse existing template engine (Parser, Compiler, TemplateCodeParser)
- Basic ConsoleOutput for success/error messages

---

### Epic 2: Project Discovery & Targeting (MVP)

Smart template and project discovery with flexible targeting options. Developer can target specific projects and rely on automatic template discovery.

**FRs covered:** FR2, FR3, FR9, FR10, FR11, FR12, FR21

**User Outcome:** Developer can process entire solutions or specific projects without manual template path specification.

**Implementation Notes:**
- `TemplateFinder` for .tst file discovery
- Project reference resolution via Buildalyzer
- `--project` argument as alternative to `--solution`

---

### Epic 3: Professional Output & Diagnostics (MVP)

Clear, actionable feedback for successful and failed generations with full scripting support.

**FRs covered:** FR13, FR14, FR15, FR16, FR17, FR18, FR19, FR36, FR37, FR39

**User Outcome:** Developer can quickly identify and fix issues when generation fails, and integrate CLI into scripts.

**Implementation Notes:**
- Compiler-style error format: `File:Line:Column: Message`
- Warning vs error distinction
- Execution timing
- Exit codes: 1 for generation failure, 2 for invalid args
- stderr for errors, stdout for normal output

---

### Epic 4: Configuration Files (Growth)

Team-shareable configuration for standardized workflows. Team lead commits `.typewriterrc` and entire team uses consistent settings.

**FRs covered:** FR22, FR25, FR26, FR27, FR28, FR29, FR30

**User Outcome:** Teams can standardize CLI workflows with committed configuration files.

**Implementation Notes:**
- `CliSettings` model with Newtonsoft.Json
- Config discovery: current directory → solution root
- CLI args override config values
- Zero-argument execution when config exists

---

### Epic 5: Advanced Output Modes (Growth)

Flexible output formatting for CI/CD pipelines, debugging, and preview workflows.

**FRs covered:** FR31, FR32, FR33, FR34

**User Outcome:** Power users and CI/CD pipelines get machine-parseable output and safe preview modes.

**Implementation Notes:**
- `--quiet`: suppress non-error output
- `--verbose`: file-by-file detail
- `--json`: machine-parseable format
- `--dry-run`: preview without writing

---

## Epic 1: Generate Command Foundation (MVP)

Developer runs `typewriter generate --solution ./MyProject.sln` and TypeScript files appear. This is the core promise - TypeScript generation without Visual Studio.

---

### Story 1.1: CLI Project Setup & Command Structure

As a **developer**,
I want **a Typewriter CLI executable with help and version commands**,
So that **I can discover available options and verify the installed version**.

**Acceptance Criteria:**

**Given** the Typewriter.CLI project does not exist
**When** the project is created following architecture specifications
**Then** `src/CLI/Typewriter.CLI.csproj` exists targeting .NET Framework 4.7.2
**And** the project references `Typewriter.CodeModel`, `Typewriter.Metadata`, and `Typewriter.Metadata.Roslyn`
**And** the project references `System.CommandLine` for argument parsing
**And** the project builds successfully as part of `Typewriter.sln`

**Given** the CLI executable exists
**When** a user runs `typewriter --help`
**Then** help text displays showing available commands and options
**And** the `generate` command is listed with its description

**Given** the CLI executable exists
**When** a user runs `typewriter --version`
**Then** the version number is displayed (e.g., "Typewriter CLI v1.0.0")

**Given** the CLI executable exists
**When** a user runs `typewriter generate --help`
**Then** help text displays showing the `--solution` option and its description

**FRs Covered:** FR23, FR24

---

### Story 1.2: Standalone Workspace Provider

As a **developer**,
I want **to specify a solution file path and have the CLI load it without Visual Studio**,
So that **I can use Typewriter from any editor or terminal**.

**Acceptance Criteria:**

**Given** a valid .sln file path
**When** `CliMetadataProvider` is instantiated with that path
**Then** the solution is loaded using Buildalyzer
**And** an AdhocWorkspace is created with the solution's projects
**And** no Visual Studio dependencies are required

**Given** the CLI executable exists
**When** a user runs `typewriter generate --solution ./path/to/Solution.sln`
**Then** the solution file is located and validated
**And** the `CliMetadataProvider` loads the solution successfully

**Given** an invalid or non-existent solution path
**When** a user runs `typewriter generate --solution ./invalid/path.sln`
**Then** an error message is displayed indicating the file was not found
**And** the CLI exits with a non-zero exit code

**Given** a solution file that cannot be parsed
**When** `CliMetadataProvider` attempts to load it
**Then** an error message is displayed with actionable information
**And** the error does not crash the application (graceful handling)

**FRs Covered:** FR8, FR20

---

### Story 1.3: Template Engine Integration

As a **developer**,
I want **the CLI to discover and process all .tst templates in my solution**,
So that **TypeScript is generated from my C# code using my existing templates**.

**Acceptance Criteria:**

**Given** a solution with one or more `.tst` template files
**When** `typewriter generate --solution ./Solution.sln` is executed
**Then** all `.tst` files in the solution are discovered
**And** each template is processed by the existing template engine

**Given** a template with standard Typewriter syntax (`$Classes`, `$Properties`, etc.)
**When** the template is processed
**Then** the C# code model is extracted via `CliMetadataProvider`
**And** the template produces TypeScript output

**Given** a template with custom C# code blocks (`${...}` syntax)
**When** the template is processed
**Then** the custom C# code executes correctly
**And** the output matches what the VS extension would produce

**Given** a template with `#reference` directives
**When** the template is processed
**Then** the referenced files are resolved relative to the template location
**And** the template compiles and executes successfully

**Given** a template that references types from multiple projects
**When** the template is processed
**Then** types from all referenced projects are available in the code model

**FRs Covered:** FR1, FR5, FR6, FR7

---

### Story 1.4: Generation Execution & Output Parity

As a **developer**,
I want **the CLI to write generated TypeScript files and confirm success**,
So that **I can trust the output matches Visual Studio and use the CLI in scripts**.

**Acceptance Criteria:**

**Given** templates are processed successfully
**When** generation completes
**Then** TypeScript files are written to the locations specified by templates
**And** a success message displays the count of generated files
**And** the CLI exits with code 0

**Given** identical C# source files and .tst templates
**When** generation is run via CLI and via VS extension
**Then** the generated TypeScript files are byte-identical
**And** file paths match exactly

**Given** generation is run in a script or CI environment
**When** the CLI executes
**Then** no interactive prompts or confirmations are displayed
**And** the process completes without user intervention

**Given** generation completes successfully
**When** the CLI exits
**Then** stdout contains the success summary
**And** the exit code is 0

**Given** the same inputs are provided multiple times
**When** generation is run repeatedly
**Then** output is deterministic (identical each time)

**FRs Covered:** FR4, FR35, FR38

---

## Epic 2: Project Discovery & Targeting (MVP)

Smart template and project discovery with flexible targeting options. Developer can target specific projects and rely on automatic template discovery.

---

### Story 2.1: Automatic Template Discovery

As a **developer**,
I want **the CLI to automatically find all .tst templates in my solution**,
So that **I don't have to manually specify each template path**.

**Acceptance Criteria:**

**Given** a solution with `.tst` files in various project directories
**When** `typewriter generate --solution ./Solution.sln` is executed
**Then** all `.tst` files within the solution's project directories are discovered
**And** templates in nested folders are included
**And** templates in `obj/` and `bin/` directories are excluded

**Given** a solution with no `.tst` files
**When** generation is attempted
**Then** a warning message indicates no templates were found
**And** the CLI exits gracefully (not an error)

**Given** a solution with templates in multiple projects
**When** `TemplateFinder` scans the solution
**Then** templates from all projects are collected
**And** the template's project context is preserved for code model resolution

**FRs Covered:** FR10

---

### Story 2.2: Project-Specific Targeting

As a **developer**,
I want **to target a specific project instead of the entire solution**,
So that **I can generate TypeScript for just the project I'm working on**.

**Acceptance Criteria:**

**Given** the CLI executable exists
**When** a user runs `typewriter generate --project ./path/to/Project.csproj`
**Then** only that project and its dependencies are loaded
**And** only templates within that project are processed

**Given** a valid .csproj file path
**When** `CliMetadataProvider` is instantiated with that path
**Then** the project is loaded using Buildalyzer
**And** project references are resolved for type availability

**Given** both `--solution` and `--project` are specified
**When** the CLI parses arguments
**Then** an error indicates these options are mutually exclusive
**And** the CLI exits with code 2

**Given** an invalid or non-existent project path
**When** a user runs `typewriter generate --project ./invalid/path.csproj`
**Then** an error message is displayed indicating the file was not found
**And** the CLI exits with a non-zero exit code

**FRs Covered:** FR3, FR9, FR21

---

### Story 2.3: Solution-Wide Generation with Reference Resolution

As a **developer**,
I want **all templates in my solution processed with full type resolution across projects**,
So that **templates can reference types from any project in the solution**.

**Acceptance Criteria:**

**Given** a solution with multiple projects
**When** `typewriter generate --solution ./Solution.sln` is executed
**Then** all projects in the solution are loaded into the workspace
**And** templates in every project are discovered and processed

**Given** a template in Project A that references a type from Project B
**When** the template is processed
**Then** the type from Project B is available in the code model
**And** the template generates correctly

**Given** a project with NuGet package references
**When** the project is loaded
**Then** types from referenced packages are available for template processing

**Given** a project with project-to-project references
**When** `CliMetadataProvider` loads the solution
**Then** the reference chain is resolved
**And** all transitively referenced types are available

**Given** C# source files referenced by templates
**When** the code model is extracted
**Then** classes, interfaces, enums, and records are available
**And** properties, methods, and attributes are accessible

**FRs Covered:** FR2, FR11, FR12

---

## Epic 3: Professional Output & Diagnostics (MVP)

Clear, actionable feedback for successful and failed generations with full scripting support.

---

### Story 3.1: Generation Summary & Timing

As a **developer**,
I want **a clear summary of what was generated and how long it took**,
So that **I can verify the generation completed successfully**.

**Acceptance Criteria:**

**Given** generation completes successfully
**When** the CLI outputs results
**Then** the summary shows the total count of generated TypeScript files
**And** the list of templates that were processed is displayed
**And** the total execution time is shown (e.g., "Completed in 1.2s")

**Given** the CLI starts execution
**When** the banner is displayed
**Then** version information is shown (e.g., "Typewriter CLI v1.0.0")
**And** the solution/project being processed is indicated

**Given** multiple templates are processed
**When** generation completes
**Then** each template's contribution to the output is visible
**And** the user can see which templates generated which files

**Example output format:**
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

**FRs Covered:** FR13, FR14, FR18, FR19

---

### Story 3.2: Error Reporting with File Location

As a **developer**,
I want **error messages that show exactly where the problem is**,
So that **I can quickly fix template or C# issues**.

**Acceptance Criteria:**

**Given** a template with a syntax error
**When** the template fails to compile
**Then** the error message includes the template file path
**And** the line number and column are shown
**And** the error description is actionable

**Error format:**
```
Error: Template compilation failed
  CustomerModel.tst:15:8: Cannot resolve type 'OrderStatus'
```

**Given** a C# file with analysis errors
**When** the code model extraction fails
**Then** the error message identifies the C# file
**And** the nature of the analysis error is described
**And** suggested fixes are provided when possible

**Given** multiple errors occur during generation
**When** errors are reported
**Then** all errors are displayed (not just the first one)
**And** errors are grouped by file for readability

**Given** an error occurs in a referenced file
**When** the error is reported
**Then** the reference chain is shown (which template referenced which file)

**FRs Covered:** FR15, FR16

---

### Story 3.3: Warning vs Error Distinction

As a **developer**,
I want **to distinguish between blocking errors and non-blocking warnings**,
So that **generation can proceed when issues are minor**.

**Acceptance Criteria:**

**Given** a non-critical issue occurs (e.g., file not in solution tree)
**When** the issue is logged
**Then** it is displayed as a warning (yellow color)
**And** generation continues for other templates
**And** the final exit code is still 0 if all templates succeeded

**Given** a critical issue occurs (e.g., template compilation failure)
**When** the issue is logged
**Then** it is displayed as an error (red color)
**And** that template is skipped
**And** the final exit code is 1 if any errors occurred

**Given** both warnings and errors occur
**When** the summary is displayed
**Then** the count of warnings and errors is shown separately
**And** the exit code reflects whether any errors occurred

**Given** a warning-only generation run
**When** generation completes
**Then** the exit code is 0
**And** the warnings are still visible in output

**FRs Covered:** FR17

---

### Story 3.4: Scripting Integration & Stream Separation

As a **developer**,
I want **proper exit codes and stream separation for scripting**,
So that **I can use the CLI in build scripts and CI/CD pipelines**.

**Acceptance Criteria:**

**Given** generation fails due to template errors
**When** the CLI exits
**Then** the exit code is 1
**And** error messages are written to stderr

**Given** invalid command line arguments are provided
**When** the CLI parses arguments
**Then** the exit code is 2
**And** the error message explains the invalid argument

**Given** generation succeeds
**When** the CLI exits
**Then** the exit code is 0
**And** success output is written to stdout

**Given** errors and normal output both occur
**When** the CLI runs
**Then** errors go to stderr
**And** normal output goes to stdout
**And** these can be redirected separately in scripts

**Given** a CI/CD pipeline runs the CLI
**When** the pipeline checks the exit code
**Then** exit code 0 indicates success (pipeline continues)
**And** exit code 1 indicates generation failure (pipeline can fail the build)
**And** exit code 2 indicates configuration error (pipeline can fail early)

**FRs Covered:** FR36, FR37, FR39

---

## Epic 4: Configuration Files (Growth)

Team-shareable configuration for standardized workflows. Team lead commits `.typewriterrc` and entire team uses consistent settings.

---

### Story 4.1: Configuration File Support

As a **team lead**,
I want **to create a configuration file that the CLI automatically discovers**,
So that **my team can run `typewriter generate` with zero arguments**.

**Acceptance Criteria:**

**Given** a `.typewriterrc` file exists in the current directory
**When** `typewriter generate` is run without arguments
**Then** the configuration is loaded from `.typewriterrc`
**And** generation proceeds using the configured solution path

**Given** a `typewriter.json` file exists in the current directory
**When** `typewriter generate` is run without arguments
**Then** the configuration is loaded from `typewriter.json`
**And** `.typewriterrc` takes precedence if both exist

**Given** no config file in current directory but one exists in solution root
**When** `typewriter generate` is run
**Then** the config file is discovered by walking up the directory tree
**And** the first config file found is used

**Given** the `--config` argument is provided
**When** `typewriter generate --config ./custom/config.json` is run
**Then** the specified config file is used
**And** auto-discovery is skipped

**Given** no config file exists and no arguments provided
**When** `typewriter generate` is run
**Then** an error indicates no solution/project specified
**And** the CLI exits with code 2

**Config file format (JSON):**
```json
{
  "solution": "./MyProject.sln",
  "templates": ["**/*.tst"],
  "verbosity": "normal"
}
```

**FRs Covered:** FR22, FR25, FR26

---

### Story 4.2: Configuration Options & CLI Override

As a **developer**,
I want **to configure verbosity and template patterns in the config file**,
So that **I can customize behavior without remembering CLI flags**.

**Acceptance Criteria:**

**Given** a config file with `"verbosity": "verbose"`
**When** generation runs
**Then** verbose output is displayed (file-by-file details)

**Given** a config file with template include/exclude patterns
**When** generation runs
**Then** only templates matching include patterns are processed
**And** templates matching exclude patterns are skipped

**Config with patterns:**
```json
{
  "solution": "./MyProject.sln",
  "templates": ["src/**/*.tst"],
  "exclude": ["**/test/**/*.tst"]
}
```

**Given** a config file setting and a CLI argument for the same option
**When** generation runs
**Then** the CLI argument takes precedence
**And** the config file value is overridden

**Given** `typewriter generate --solution ./Other.sln` with config specifying a different solution
**When** the CLI runs
**Then** `./Other.sln` is used (CLI wins)

**Given** a config file with an unknown property
**When** the config is loaded
**Then** a warning is displayed about the unknown property
**And** generation proceeds with known properties

**FRs Covered:** FR27, FR28, FR29, FR30

---

## Epic 5: Advanced Output Modes (Growth)

Flexible output formatting for CI/CD pipelines, debugging, and preview workflows.

---

### Story 5.1: Quiet and Verbose Output Modes

As a **developer**,
I want **to control the verbosity of CLI output**,
So that **I can get minimal output for scripts or detailed output for debugging**.

**Acceptance Criteria:**

**Given** the `--quiet` flag is provided
**When** generation runs successfully
**Then** only error messages are displayed
**And** success summary is suppressed
**And** exit code still reflects success/failure

**Given** the `--verbose` flag is provided
**When** generation runs
**Then** each file being processed is shown
**And** each file being generated is shown
**And** timing details for each step are included

**Verbose output example:**
```
Typewriter CLI v1.0.0
Processing solution: ./MyProject.sln
Loading project: MyProject.Core (0.3s)
Loading project: MyProject.Api (0.2s)
Found 3 templates

Processing: Models.tst
  Generating: Models/Customer.ts
  Generating: Models/Order.ts
Processing: Enums.tst
  Generating: Enums/OrderStatus.ts
...

Generated 12 TypeScript files in 1.2s
```

**Given** both `--quiet` and `--verbose` are provided
**When** the CLI parses arguments
**Then** an error indicates these options are mutually exclusive
**And** the CLI exits with code 2

**FRs Covered:** FR31, FR32

---

### Story 5.2: JSON Output and Dry Run

As a **DevOps engineer**,
I want **machine-parseable JSON output and a dry-run preview mode**,
So that **I can integrate the CLI into automated tooling and preview changes safely**.

**Acceptance Criteria:**

**Given** the `--json` flag is provided
**When** generation completes
**Then** output is formatted as JSON
**And** the JSON includes success status, file list, and timing

**JSON output format:**
```json
{
  "success": true,
  "version": "1.0.0",
  "solution": "./MyProject.sln",
  "templatesProcessed": 3,
  "filesGenerated": 12,
  "files": [
    "Models/Customer.ts",
    "Models/Order.ts"
  ],
  "warnings": [],
  "errors": [],
  "duration": "1.2s"
}
```

**Given** the `--json` flag with errors
**When** generation fails
**Then** errors are included in the JSON structure
**And** `success` is `false`

**Given** the `--dry-run` flag is provided
**When** generation runs
**Then** templates are processed but no files are written
**And** output shows what would have been generated
**And** existing files are not modified

**Dry-run output:**
```
Typewriter CLI v1.0.0 (dry-run)
Processing solution: ./MyProject.sln

Would generate 12 TypeScript files:
  - Models/Customer.ts (new)
  - Models/Order.ts (modified)
  ...

No files were written.
```

**Given** `--dry-run` and `--json` together
**When** generation runs
**Then** JSON output indicates dry-run mode
**And** no files are written

**FRs Covered:** FR33, FR34

