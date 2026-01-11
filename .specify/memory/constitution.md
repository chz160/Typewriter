<!--
Sync Impact Report
==================
Version change: 0.0.0 → 1.0.0 (MAJOR - initial ratification)
Modified principles: N/A (new constitution)
Added sections:
  - Core Principles (5 principles)
  - Upstream Compatibility Requirements
  - Development Workflow
  - Governance
Removed sections: N/A
Templates requiring updates:
  - .specify/templates/plan-template.md: ✅ No updates needed (Constitution Check placeholder compatible)
  - .specify/templates/spec-template.md: ✅ No updates needed (requirements format compatible)
  - .specify/templates/tasks-template.md: ✅ No updates needed (task structure compatible)
Follow-up TODOs: None
-->

# Typewriter Fork Constitution

## Core Principles

### I. Upstream Style Conformance

All code contributions MUST adhere to the existing coding conventions established in the
upstream Typewriter repository. This is non-negotiable as we are augmenting a project that
is not ours.

**Requirements:**
- PascalCase for public classes, methods, properties, and interfaces
- Interface names MUST be prefixed with `I` (e.g., `IClassMetadata`, `IPropertyCollection`)
- Implementation classes MUST use `*Impl` suffix (e.g., `ClassImpl`, `PropertyImpl`)
- Collection classes MUST use `*Collection` or `*CollectionImpl` suffix
- Roslyn-specific implementations MUST use `Roslyn*` prefix (e.g., `RoslynClassMetadata`)
- Private fields MUST use `_camelCase` naming (leading underscore)
- Namespaces MUST follow directory structure (e.g., `Typewriter.CodeModel.Implementation`)
- XML documentation comments MUST be present on all public API members
- Using directives MUST be placed outside namespace, with System namespaces first

**Rationale:** Maintaining consistency with the upstream codebase ensures our fork remains
mergeable, reduces cognitive load for contributors familiar with the original project,
and respects the architectural decisions made by the original authors.

### II. Test Coverage Requirements

All new features and bug fixes MUST include corresponding test coverage using the
established testing patterns.

**Requirements:**
- Test framework: xUnit with `[Fact]` attributes for test methods
- Assertions: Use `Should` fluent assertion library (e.g., `.ShouldEqual()`, `.ShouldBeNull()`)
- Mocking: Use `NSubstitute` for creating test doubles
- VS extensibility tests MUST use `MefHostingFixture` and `[Collection(MockedVS.Collection)]`
- Abstract test base classes SHOULD be used when testing multiple implementations
- Test classes MUST inherit from `TestInfrastructure.TestBase` when async lifecycle needed
- Test class naming: `{FeatureName}Tests` or `Roslyn{FeatureName}Tests` for Roslyn-specific
- Test method naming: `Expect_{expected_behavior}_when_{condition}` pattern preferred
- New metadata interfaces MUST have corresponding Roslyn implementation tests

**Rationale:** The codebase relies on comprehensive testing to validate Roslyn analysis
correctness. Template generation is complex and subtle bugs can produce incorrect
TypeScript output that breaks downstream applications.

### III. Architectural Consistency

All code MUST follow the established layered architecture pattern separating abstract
models, metadata interfaces, and concrete implementations.

**Requirements:**
- Abstract code model types go in `src/CodeModel/` (e.g., `Class.cs`, `Property.cs`)
- Metadata interfaces go in `src/Metadata/Interfaces/` (e.g., `IClassMetadata.cs`)
- Roslyn implementations go in `src/Roslyn/` (e.g., `RoslynClassMetadata.cs`)
- Implementation classes go in `src/Typewriter/CodeModel/Implementation/` (e.g., `ClassImpl.cs`)
- Collection implementations go in `src/Typewriter/CodeModel/Collections/`
- New features MUST use the static factory method pattern (`FromMetadata`)
- Lazy initialization pattern MUST be used for expensive property calculations
- Parent/child relationships MUST be maintained via `Item parent` constructor parameter
- Settings MUST be threaded through all implementation constructors

**Rationale:** The three-layer architecture (abstract model → metadata interface → implementation)
enables the codebase to support multiple code analysis backends while presenting a unified
API to template authors.

### IV. Visual Studio Extension Compatibility

All changes MUST maintain compatibility with the supported Visual Studio versions and
adhere to VS extensibility best practices.

**Requirements:**
- Target framework MUST remain .NET Framework 4.7.2 (VS extension requirement)
- VS 2022 (v17.x) compatibility MUST be maintained
- MEF exports MUST be properly attributed for VS service discovery
- UI operations MUST marshal to the UI thread via `ThreadHelper`
- Solution and project events MUST be handled through proper VS service interfaces
- Template editor features MUST follow VS editor extensibility patterns
- Extension package initialization MUST be async where supported
- No dependencies on packages incompatible with .NET Framework 4.7.2

**Rationale:** Typewriter is a Visual Studio extension. Breaking VS compatibility or
using improper threading patterns will cause runtime failures, hangs, or crashes
within the IDE environment.

### V. Performance and Resource Efficiency

Code MUST be designed for efficient operation within the Visual Studio environment
where responsiveness and resource usage directly impact user experience.

**Requirements:**
- Lazy loading MUST be used for all expensive computations (Roslyn symbol resolution)
- Caching MUST be implemented for repeated metadata lookups within a generation cycle
- File I/O operations MUST handle long paths when `LongPathsEnabled` registry key is set
- Template compilation results SHOULD be cached when templates haven't changed
- Roslyn workspace operations MUST avoid blocking the UI thread
- Memory allocations in hot paths (file watching, change detection) SHOULD be minimized
- Generated file writes MUST respect BOM configuration settings
- Solution-wide operations MUST support cancellation tokens where applicable

**Rationale:** Visual Studio users expect their IDE to remain responsive. Poor performance
in an extension degrades the entire development experience and leads to user frustration
and extension uninstallation.

## Upstream Compatibility Requirements

As a fork of the AdaskoTheBeAsT/Typewriter repository (itself a fork of frhagn/Typewriter),
we have additional obligations:

**MUST:**
- Preserve backward compatibility with existing .tst template files
- Maintain the existing public API surface for template authors
- Document any behavioral changes that could affect existing users
- Keep the dual-naming pattern (PascalCase + camelCase) for template property access

**SHOULD:**
- Minimize divergence from upstream to facilitate future merges
- Contribute generally-useful improvements back to upstream via PRs
- Follow upstream's versioning scheme (MAJOR.MINOR.PATCH)
- Preserve existing XML documentation style and coverage

**MUST NOT:**
- Remove or rename existing public API members without deprecation period
- Change the semantic behavior of existing template functions
- Introduce breaking changes to the Settings API without major version bump

## Development Workflow

### Code Review Requirements
- All changes MUST pass existing test suite before merge
- New public API members MUST include XML documentation
- StyleCop and code analyzer warnings MUST be addressed or explicitly suppressed with justification
- Changes affecting template output MUST include sample template verification

### Quality Gates
- Build MUST succeed with `msbuild Typewriter.sln /p:Configuration=Release`
- Tests MUST pass via `dotnet test` or `vstest.console.exe`
- No new StyleCop errors without documented justification via pragma directives

### Branch Strategy
- Feature branches MUST target `master` branch
- Branch naming: `feature/description` or `fix/description`
- Commits SHOULD be atomic and focused on single logical changes

## Governance

This constitution establishes the non-negotiable principles for all contributions to
this Typewriter fork. Compliance is mandatory.

**Amendment Process:**
1. Propose changes via pull request modifying this document
2. Document rationale for the change
3. Update affected templates and guidance files
4. Increment version according to semantic versioning rules

**Versioning Policy:**
- MAJOR: Backward-incompatible principle changes or removals
- MINOR: New principles added or existing principles materially expanded
- PATCH: Clarifications, wording improvements, typo fixes

**Compliance Review:**
- All PRs MUST be checked against applicable principles
- Constitution violations MUST be resolved before merge
- Complexity beyond these principles MUST be justified in PR description

**Version**: 1.0.0 | **Ratified**: 2026-01-11 | **Last Amended**: 2026-01-11
