# Feature Specification: Typewriter CLI Extension

**Feature Branch**: `001-typewriter-cli`
**Created**: 2026-01-11
**Status**: Draft
**Input**: User description: "Add CLI support for TypeScript generation from C# code"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Generate TypeScript from VS Code Terminal (Priority: P1)

Alex is a full-stack developer working on an e-commerce platform with a C# API backend and React TypeScript frontend. The team uses Typewriter templates to keep TypeScript DTOs in sync with C# models. Currently, Alex must minimize VS Code, open Visual Studio, wait for it to load, save a file to trigger generation, then switch back. This breaks concentration and costs 2-3 minutes each time.

Alex opens the VS Code terminal and types `typewriter generate --solution ./MyProject.sln`. Within seconds, the console shows "Generated 12 TypeScript files" and Alex sees the updated interfaces in the file tree. No context switching, no waiting for Visual Studio to load.

**Why this priority**: This is the core value proposition - enabling TypeScript generation without Visual Studio. Without this capability, the product has no reason to exist.

**Independent Test**: Can be fully tested by running the CLI against any existing Typewriter solution with .tst templates. Delivers immediate value by producing TypeScript files.

**Acceptance Scenarios**:

1. **Given** a solution with C# files and .tst templates, **When** user runs `typewriter generate --solution ./MyProject.sln`, **Then** TypeScript files are generated matching the template output specifications
2. **Given** a project file instead of solution, **When** user runs `typewriter generate --project ./MyProject.csproj`, **Then** TypeScript files are generated for that project
3. **Given** a solution path, **When** generation completes successfully, **Then** console displays summary including file count and execution time
4. **Given** no explicit path specified but config file exists, **When** user runs `typewriter generate`, **Then** CLI discovers and uses the config file settings

---

### User Story 2 - Troubleshoot Template Errors (Priority: P1)

Alex pulls the latest code from the repository and runs `typewriter generate` as usual. This time, the console shows an error: "Template compilation failed: CustomerModel.tst(15,8): Cannot resolve type 'OrderStatus'". The template references an enum that was moved to a different namespace.

Alex opens the template file, sees the issue at line 15, and fixes the `using` statement. After running `typewriter generate` again, the output shows: "Generated 12 TypeScript files". The error messages were clear enough to diagnose and fix the issue without documentation.

**Why this priority**: Error handling is essential for any development tool. Without clear error messages, developers cannot effectively use or troubleshoot the CLI.

**Independent Test**: Can be tested by introducing deliberate errors in templates and verifying error output contains file path, line number, column, and actionable message.

**Acceptance Scenarios**:

1. **Given** a template with a compilation error, **When** generation runs, **Then** console displays error with file path, line number, column, and descriptive message
2. **Given** a template referencing a missing type, **When** generation runs, **Then** console displays the missing type name and template location
3. **Given** multiple errors exist, **When** generation runs, **Then** all errors are reported (not just the first one)
4. **Given** some files generate successfully and some fail, **When** generation completes, **Then** console distinguishes between warnings (non-blocking) and errors (blocking)

---

### User Story 3 - Use in CI/CD Pipeline (Priority: P2)

Jordan, the tech lead, wants to add TypeScript generation to the team's CI/CD pipeline. They need the CLI to integrate with standard automation tooling - returning proper exit codes, writing errors to stderr, and operating without prompts.

Jordan adds `typewriter generate --solution ./MyProject.sln` to the build pipeline. When generation fails, the pipeline correctly detects the failure through the non-zero exit code. Error messages appear in the build log with enough detail to diagnose issues remotely.

**Why this priority**: CI/CD integration extends the CLI's value beyond individual developers to team workflows. While not strictly MVP, it requires minimal additional work since the underlying behavior is needed for P1 stories.

**Independent Test**: Can be tested by running CLI in automated scripts and verifying exit codes match documented behavior.

**Acceptance Scenarios**:

1. **Given** successful generation, **When** CLI completes, **Then** exit code is 0
2. **Given** template compilation failure, **When** CLI completes, **Then** exit code is 1
3. **Given** invalid arguments or missing files, **When** CLI completes, **Then** exit code is 2
4. **Given** any error, **When** CLI writes output, **Then** errors go to stderr and success output goes to stdout
5. **Given** a build environment, **When** CLI runs, **Then** no interactive prompts or confirmations are required

---

### User Story 4 - Standardize Team Workflow with Config File (Priority: P3)

Jordan wants to standardize how the team runs Typewriter. Currently, each developer runs the command with slightly different arguments. Jordan creates a `.typewriterrc` file in the repository root specifying the solution path and template locations, then commits it.

New team members now onboard with a single instruction: "Run `typewriter generate` from the repo root." No Visual Studio installation required for frontend-focused developers who only need to regenerate TypeScript after pulling changes.

**Why this priority**: Configuration files improve team consistency and onboarding but are not required for the core generation capability to work.

**Independent Test**: Can be tested by creating a config file and verifying `typewriter generate` without arguments uses those settings.

**Acceptance Scenarios**:

1. **Given** a `.typewriterrc` file in the current directory, **When** user runs `typewriter generate` without arguments, **Then** CLI uses settings from the config file
2. **Given** a `typewriter.json` file in the solution root, **When** user runs `typewriter generate`, **Then** CLI discovers and uses that config file
3. **Given** both CLI arguments and config file, **When** user runs the command, **Then** CLI arguments override config file settings
4. **Given** no config file and no arguments, **When** user runs `typewriter generate`, **Then** CLI displays helpful error about required arguments

---

### Edge Cases

- What happens when the solution file path does not exist? CLI reports clear error message with the invalid path and exits with code 2.
- What happens when the solution contains no .tst template files? CLI reports "No templates found" as a warning and exits with code 0 (not an error condition).
- What happens when a template references a C# file that was deleted? CLI reports warning for the missing file, continues processing other templates, and includes the warning in output.
- What happens when template output path is read-only or inaccessible? CLI reports error with the specific file path and permission issue.
- What happens when running in a directory without read permissions? CLI reports appropriate permission error and exits with code 2.
- What happens when the solution has project references that cannot be resolved? CLI reports warnings for unresolved references but continues processing accessible projects.

## Requirements *(mandatory)*

### Functional Requirements

**Code Generation (MVP)**
- **FR-001**: System MUST generate TypeScript files from C# source files using .tst templates
- **FR-002**: System MUST produce byte-identical TypeScript output compared to the Visual Studio extension given the same inputs
- **FR-003**: System MUST process all .tst template files found within the specified solution or project
- **FR-004**: System MUST execute custom C# code blocks embedded in templates (${...} syntax)
- **FR-005**: System MUST resolve template references (#reference directives) relative to template location

**Project Discovery (MVP)**
- **FR-006**: Users MUST be able to specify a solution file path via `--solution` argument
- **FR-007**: Users MUST be able to specify a project file path via `--project` argument as an alternative to solution
- **FR-008**: System MUST automatically discover all .tst template files within the solution/project scope
- **FR-009**: System MUST load C# source files referenced by templates for code model extraction
- **FR-010**: System MUST resolve project references to include referenced project types in the code model

**Output & Diagnostics (MVP)**
- **FR-011**: System MUST display a summary of generated files after successful execution
- **FR-012**: System MUST display which template files were processed
- **FR-013**: System MUST display template compilation errors with file path, line number, and column
- **FR-014**: System MUST display C# analysis errors with actionable messages
- **FR-015**: System MUST distinguish between errors (blocking) and warnings (non-blocking)
- **FR-016**: System MUST display total execution time after completion
- **FR-017**: System MUST output version information on startup

**CLI Interface (MVP)**
- **FR-018**: Users MUST be able to request help via `--help` argument
- **FR-019**: Users MUST be able to request version information via `--version` argument
- **FR-020**: System MUST exit with code 0 on successful generation
- **FR-021**: System MUST exit with code 1 on generation failure (template errors, missing files)
- **FR-022**: System MUST exit with code 2 on invalid arguments or configuration
- **FR-023**: System MUST operate non-interactively (no prompts or confirmations required)
- **FR-024**: System MUST write errors to stderr and normal output to stdout

**Configuration (Growth Phase)**
- **FR-025**: Users MUST be able to create a `.typewriterrc` or `typewriter.json` config file for default settings
- **FR-026**: System MUST discover config file in current directory or solution root automatically
- **FR-027**: CLI arguments MUST override config file settings when both are provided
- **FR-028**: Users MUST be able to run `typewriter generate` with zero arguments when config file exists

**Output Modes (Growth Phase)**
- **FR-029**: Users MUST be able to suppress non-error output via `--quiet` flag
- **FR-030**: Users MUST be able to enable detailed file-by-file output via `--verbose` flag
- **FR-031**: Users MUST be able to preview generation without writing files via `--dry-run` flag

### Key Entities

- **Template (.tst file)**: Typewriter template file containing patterns for TypeScript generation, including C# code blocks and item filters. Located within solution/project directory structure.
- **C# Source File**: Input file analyzed by Roslyn to extract metadata (classes, interfaces, enums, properties, methods) for template processing.
- **Generated TypeScript File**: Output file produced by template processing, written to location specified by template output directives.
- **Solution/Project**: MSBuild solution (.sln) or project (.csproj) file that defines the scope of C# files to analyze and templates to process.
- **Configuration File**: Optional JSON file (`.typewriterrc` or `typewriter.json`) containing default CLI settings for team standardization.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can generate TypeScript files from C# code without launching Visual Studio
- **SC-002**: Running `typewriter generate` with solution path produces TypeScript files with a single command execution
- **SC-003**: Generated TypeScript output is byte-identical to Visual Studio extension output for the same inputs
- **SC-004**: Existing .tst templates work unchanged - no template modifications required
- **SC-005**: Template compilation errors display file path, line number, and column for diagnosis
- **SC-006**: CLI cold start completes in under 3 seconds for typical solutions
- **SC-007**: Per-file generation completes in under 500ms per C# file
- **SC-008**: Solution loading completes in under 30 seconds for solutions with up to 100 projects
- **SC-009**: Memory usage remains under 2GB for typical solutions
- **SC-010**: Same inputs always produce identical outputs (deterministic generation)

## Assumptions

- Users have .NET 8 runtime installed on their system (required for CLI execution)
- Users have existing Typewriter .tst templates already working with the Visual Studio extension
- Solution/project files follow standard MSBuild format (VS 2019/2022 compatible)
- Template output paths are writable by the user executing the CLI
- C# source files are syntactically valid (parseable by Roslyn)
