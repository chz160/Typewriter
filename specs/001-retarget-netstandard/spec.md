# Feature Specification: Retarget CodeModel and Metadata to .NET Standard 2.0

**Feature Branch**: `001-retarget-netstandard`
**Created**: 2026-01-11
**Status**: Draft
**Input**: Retarget Typewriter.CodeModel and Typewriter.Metadata to netstandard2.0 to enable cross-platform consumption

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Build Shared Libraries for CLI Tool (Priority: P1)

As a developer building the Typewriter CLI tool targeting .NET 8, I need the CodeModel and
Metadata libraries to target .NET Standard 2.0 so that I can reference them from my modern
.NET project without compatibility issues.

**Why this priority**: This is the core enabler for the CLI tool initiative. Without
cross-platform compatible libraries, the CLI cannot share code with the VS extension.

**Independent Test**: Can be fully tested by creating a .NET 8 console application that
references the retargeted libraries and successfully compiles.

**Acceptance Scenarios**:

1. **Given** Typewriter.CodeModel targets netstandard2.0, **When** a .NET 8 project adds a
   reference to it, **Then** the project builds successfully without compatibility warnings.

2. **Given** Typewriter.Metadata targets netstandard2.0, **When** a .NET 8 project adds a
   reference to it, **Then** the project builds successfully and can use all interface types.

3. **Given** both libraries are retargeted, **When** a developer creates a new .NET 8 CLI
   tool referencing both, **Then** they can instantiate and work with CodeModel types.

---

### User Story 2 - Maintain VS Extension Compatibility (Priority: P1)

As a maintainer of the Typewriter VS extension, I need the retargeted libraries to remain
compatible with the .NET Framework 4.7.2 extension so that existing functionality continues
to work without modification.

**Why this priority**: Breaking the existing VS extension would be unacceptable. This is
equally critical as enabling CLI support.

**Independent Test**: Can be fully tested by building the complete solution and running
all existing unit tests to verify no regressions.

**Acceptance Scenarios**:

1. **Given** CodeModel and Metadata target netstandard2.0, **When** the Typewriter VS
   extension project (net472) is built, **Then** it compiles successfully with no errors.

2. **Given** the solution is built, **When** all existing unit tests are executed,
   **Then** all tests pass with the same results as before migration.

3. **Given** Typewriter.Metadata.Roslyn references the retargeted Metadata library,
   **When** the Roslyn project is built, **Then** it compiles successfully.

---

### User Story 3 - SDK-Style Project Format (Priority: P2)

As a contributor to the Typewriter project, I want the CodeModel and Metadata projects
to use the modern SDK-style project format so that the project files are simpler to
maintain and support modern .NET tooling.

**Why this priority**: While not blocking functionality, SDK-style projects are required
for netstandard2.0 targeting and provide better developer experience.

**Independent Test**: Can be tested by verifying the .csproj files use SDK-style format
and support `dotnet build` commands directly.

**Acceptance Scenarios**:

1. **Given** CodeModel uses SDK-style format, **When** `dotnet build` is run on it,
   **Then** the project builds successfully without requiring Visual Studio.

2. **Given** Metadata uses SDK-style format, **When** `dotnet restore` is run,
   **Then** NuGet packages are restored correctly.

---

### Edge Cases

- What happens when dependent projects have binding redirect requirements?
  - The consuming net472 projects should handle binding redirects automatically via MSBuild
- How does the system handle if implicit usings cause compilation errors?
  - Implicit usings will not be enabled; explicit using statements will be preserved
- What happens if a System.* API used in the code is not available in netstandard2.0?
  - Analysis confirms all APIs used are available; any unexpected issues will require
    adding explicit NuGet package references (e.g., System.Xml.ReaderWriter)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Typewriter.CodeModel project MUST target `netstandard2.0` framework
- **FR-002**: Typewriter.Metadata project MUST target `netstandard2.0` framework
- **FR-003**: Both projects MUST use SDK-style .csproj format
- **FR-004**: Both projects MUST be buildable using `dotnet build` command
- **FR-005**: Both projects MUST remain referenceable by .NET Framework 4.7.2 projects
- **FR-006**: Project references between Metadata and CodeModel MUST work correctly
- **FR-007**: All existing source files MUST compile without modification (unless API
  incompatibilities are discovered)
- **FR-008**: Assembly names and root namespaces MUST remain unchanged
- **FR-009**: GenerateAssemblyInfo MUST be disabled to preserve existing AssemblyInfo.cs files

### Key Entities

- **Typewriter.CodeModel**: Data model library containing abstract types representing C#
  code elements (Class, Property, Method, Enum, Interface, Record). No external dependencies.

- **Typewriter.Metadata**: Interface library defining metadata provider contracts
  (IClassMetadata, IPropertyMetadata, etc.). Depends only on Typewriter.CodeModel.

### Assumptions

- All APIs used in CodeModel and Metadata are available in .NET Standard 2.0 (verified
  through code analysis: only System, System.Collections.Generic, System.Linq, System.Text)
- No external NuGet packages are required by either project
- The legacy project format files can be replaced entirely (no need for dual targeting)
- Existing AssemblyInfo.cs files will be preserved via GenerateAssemblyInfo=false

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Full solution builds with zero errors using
  `msbuild Typewriter.sln /p:Configuration=Release`
- **SC-002**: All existing unit tests pass (same pass count as before migration)
- **SC-003**: Both retargeted projects build independently using `dotnet build` command
- **SC-004**: A new .NET 8 console application can successfully reference and use both
  libraries (verification test)
- **SC-005**: No new compiler warnings related to framework targeting appear
- **SC-006**: VS extension project builds and references the netstandard2.0 assemblies
- **SC-007**: Typewriter.Metadata.Roslyn project builds successfully with the retargeted
  dependencies
