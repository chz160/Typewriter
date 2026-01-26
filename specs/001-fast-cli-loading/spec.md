# Feature Specification: Fast CLI Project Loading

**Feature Branch**: `001-fast-cli-loading`
**Created**: 2026-01-23
**Status**: Draft
**Input**: User description: "Fast CLI Project Loading - optimize project and solution loading performance for CLI"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Single Project Fast Loading (Priority: P1)

As a developer using the Typewriter CLI on a single project, I want project loading to complete in under 5 seconds so that I can use the CLI in my iterative development workflow without waiting.

**Why this priority**: This is the most common use case - developers working on a single project need fast feedback during development. The 2+ minute load time currently makes the CLI impractical for iterative workflows.

**Independent Test**: Can be fully tested by running the CLI against a project with 700 source files and measuring the time from command invocation to first template processing. Delivers immediate value for the majority of CLI users.

**Acceptance Scenarios**:

1. **Given** a project with 700 C# source files, **When** the user runs the CLI generate command, **Then** the project loads and template processing begins within 5 seconds.

2. **Given** a project with SDK-style .csproj format, **When** the user runs the CLI generate command, **Then** the project loads successfully using the fast loading approach.

3. **Given** a project with legacy .csproj format, **When** the user runs the CLI generate command, **Then** the project loads successfully using the fast loading approach.

4. **Given** a project with source file includes and excludes in the .csproj, **When** the user runs the CLI generate command, **Then** only the included source files are loaded for template processing.

---

### User Story 2 - Solution Fast Loading (Priority: P2)

As a developer working with a multi-project solution, I want solution loading to complete in under 30 seconds so that CI/CD pipelines and local generation remain practical.

**Why this priority**: Solution-level generation is essential for CI/CD pipelines and full codebase generation. While less frequent than single-project use, this is critical for build automation.

**Independent Test**: Can be fully tested by running the CLI against a solution with multiple projects totaling 700+ source files and measuring load time. Delivers value for CI/CD pipelines and enterprise users.

**Acceptance Scenarios**:

1. **Given** a solution with multiple projects totaling 700+ source files, **When** the user runs the CLI generate command on the solution, **Then** all projects load and template processing begins within 30 seconds.

2. **Given** a solution with project references between projects, **When** the user runs the CLI generate command, **Then** type information from referenced projects is available to templates.

3. **Given** a solution with mixed SDK-style and legacy projects, **When** the user runs the CLI generate command, **Then** all projects load successfully.

---

### User Story 3 - Graceful Fallback (Priority: P3)

As a developer using the CLI, I want the system to fall back to the slower loading method when fast loading fails so that generation still completes successfully with a warning.

**Why this priority**: Reliability is essential - users should never be blocked from generating TypeScript. A slower successful generation is better than a fast failure.

**Independent Test**: Can be fully tested by introducing conditions that cause fast loading to fail (e.g., malformed project file) and verifying the system falls back to slow loading with a warning message.

**Acceptance Scenarios**:

1. **Given** a project where fast loading fails due to unsupported project features, **When** the user runs the CLI generate command, **Then** the system falls back to slow loading, displays a warning, and generation completes successfully.

2. **Given** a project that succeeds with slow loading but fails with fast loading, **When** the user runs the CLI generate command, **Then** the warning message indicates the reason for fallback.

3. **Given** the --verbose flag is used, **When** fallback occurs, **Then** detailed information about the fallback reason is displayed.

---

### User Story 4 - Template Compatibility (Priority: P1)

As a developer with existing .tst templates, I want my templates to continue working without modification after the performance improvements so that I don't need to update my templates or regenerate my TypeScript files.

**Why this priority**: Breaking existing templates would create significant user friction and migration cost. Template compatibility is a hard requirement.

**Independent Test**: Can be fully tested by running the same templates against both the current (slow) and new (fast) loading methods and comparing the generated TypeScript output byte-for-byte.

**Acceptance Scenarios**:

1. **Given** an existing .tst template using class metadata (name, properties, methods), **When** the user runs the CLI with fast loading, **Then** the generated TypeScript is identical to the output from slow loading.

2. **Given** a template accessing attribute metadata, **When** the user runs the CLI with fast loading, **Then** attribute information is correctly available to the template.

3. **Given** a template accessing generic type information, **When** the user runs the CLI with fast loading, **Then** generic type parameters and constraints are correctly available.

4. **Given** a template accessing nullability annotations, **When** the user runs the CLI with fast loading, **Then** nullable reference type information is correctly available.

5. **Given** a template accessing inheritance hierarchy, **When** the user runs the CLI with fast loading, **Then** base classes and interfaces are correctly available.

---

### Edge Cases

- What happens when a project file contains syntax errors? The system should report the error clearly and either skip that project or fall back to slow loading.
- What happens when source files are outside the project directory? Referenced files should be loaded if they are included in the project.
- What happens when obj/ or bin/ directories contain .cs files? These directories should be excluded automatically.
- What happens when a project references NuGet packages with source generators? Source-generated files should be handled appropriately.
- What happens when conditional compilation symbols affect which files are included? The system should respect the default configuration or allow configuration specification.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST load a single project with 700 source files in under 5 seconds on typical developer hardware.
- **FR-002**: System MUST load a solution with multiple projects totaling 700+ source files in under 30 seconds.
- **FR-003**: System MUST parse SDK-style .csproj files to determine included source files.
- **FR-004**: System MUST parse legacy .csproj files with explicit file includes to determine source files.
- **FR-005**: System MUST respect Include, Exclude, and Remove patterns in project files for source file discovery.
- **FR-006**: System MUST automatically exclude obj/ and bin/ directories from source file discovery.
- **FR-007**: System MUST resolve project references within a solution to provide cross-project type information.
- **FR-008**: System MUST provide identical code model information to templates as the current implementation (classes, properties, methods, attributes, inheritance, generics, nullability).
- **FR-009**: System MUST fall back to the existing slow loading method when fast loading fails.
- **FR-010**: System MUST display a warning message when falling back to slow loading.
- **FR-011**: System MUST produce byte-identical TypeScript output compared to the current implementation for the same inputs.
- **FR-012**: System MUST support the --verbose flag to show detailed loading progress and timing information.

### Key Entities

- **Project**: Represents a .csproj file with its source file patterns, references, and build configuration. Contains multiple source files and may reference other projects.
- **Solution**: Represents a .sln file containing multiple projects with their interdependencies.
- **Source File**: A .cs file to be analyzed for code model extraction. Discovered through project file patterns or explicit includes.
- **Code Model**: The semantic representation of C# code elements (classes, properties, methods, etc.) provided to templates.
- **Loading Strategy**: The approach used to load project metadata - either fast (direct parsing) or slow (full build via Buildalyzer).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Single project loading completes in under 5 seconds for projects with up to 700 source files.
- **SC-002**: Solution loading completes in under 30 seconds for solutions with up to 700 source files across multiple projects.
- **SC-003**: 100% of existing templates produce identical output with fast loading enabled.
- **SC-004**: Fast loading succeeds for at least 90% of real-world projects without requiring fallback.
- **SC-005**: When fallback occurs, users receive a clear warning message explaining the situation.
- **SC-006**: Loading time improvement is at least 20x faster compared to the current Buildalyzer-based approach for typical projects.

## Assumptions

- "Typical developer hardware" means a modern development machine with SSD storage and at least 8GB RAM.
- The 5-second target for single projects and 30-second target for solutions are measured from CLI invocation to when template processing begins (not total generation time).
- The performance targets assume source files are on local disk, not network shares.
- The current implementation uses Buildalyzer which requires a full MSBuild evaluation; the fast approach will use direct project file parsing and Roslyn compilation without MSBuild.
- SDK-style projects use default globbing patterns (e.g., `**/*.cs`) unless explicitly overridden.
- Legacy projects have explicit `<Compile Include="..."/>` entries for each source file.
- Project references are resolved within the loaded solution; external NuGet package types are resolved through reference assemblies.

## Out of Scope

- Watch mode for continuous file monitoring (future feature)
- Incremental compilation caching between CLI runs
- Parallel template processing (orthogonal optimization)
- IDE integration improvements
- Support for F# or VB.NET projects
- Source generator output files (requires MSBuild execution)
- Conditional compilation symbol handling beyond default configuration
