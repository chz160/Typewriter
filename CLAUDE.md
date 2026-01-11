# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Typewriter is a Visual Studio extension (VSIX) that generates TypeScript files from C# code using TypeScript Templates (.tst files). It watches for C# file changes and automatically updates corresponding TypeScript files.

**Fork origin:** https://github.com/AdaskoTheBeAsT/Typewriter (fork of frhagn/Typewriter)

## Build Commands

```bash
# Build entire solution (requires Visual Studio 2022 or MSBuild)
msbuild Typewriter.sln /p:Configuration=Release

# Clean build artifacts
pwsh ./clean.ps1

# Sync git submodules (Buildalyzer)
./submodules-sync.bat

# Package VSIX
pwsh ./build/PackageReferences.ps1
```

**Output:** VSIX package at `src/Typewriter/bin/Release/Typewriter.vsix`

Since this is a .Net Framework 4.7.2 project and we want to stay at this version use msbuild.exe and vstest.console.exe build and test the project
They are located here: 
	/mnt/c/Program Files/Microsoft Visual Studio/18/Enterprise/MSBuild/Current/Bin/amd64/MSBuild.exe
	/mnt/c/Program Files/Microsoft Visual Studio/18/Enterprise/Common7/IDE/Extensions/TestPlatform/vstest.console.exe

## Running Tests

```bash
# Run all tests
dotnet test src/Tests/Typewriter.Tests.csproj

# Run specific test class
dotnet test src/Tests/Typewriter.Tests.csproj --filter "FullyQualifiedName~ClassTests"

# Run single test
dotnet test src/Tests/Typewriter.Tests.csproj --filter "FullyQualifiedName~ClassTests.Expect_name_to_match_class_name"
```

**Test framework:** xUnit with `Should` fluent assertions and `NSubstitute` for mocking.
**Test fixtures:** Use `MefHostingFixture` for VS extensibility DI testing.

## Architecture

### Core Projects

1. **Typewriter.CodeModel** (`src/CodeModel/`) - Data model representing C# code elements (Class, Property, Method, Enum, Interface, Record, etc.) with collection types and type extensions.

2. **Typewriter.Metadata** (`src/Metadata/`) - Abstract interfaces (`IClassMetadata`, `IPropertyMetadata`, etc.) defining the metadata provider contract.

3. **Typewriter.Metadata.Roslyn** (`src/Roslyn/`) - Roslyn-based implementation that analyzes C# source files using Microsoft.CodeAnalysis.

4. **Typewriter** (`src/Typewriter/`) - Main VS extension containing:
   - `CodeModel/` - Implementation classes (`ClassImpl`, `PropertyImpl`, etc.)
   - `Generation/` - Template engine (`Template`, `Parser`, `Compiler`, `TemplateCodeParser`)
   - `TemplateEditor/` - Editor features (syntax highlighting, code completion, outlining)
   - `VisualStudio/` - VS integration (`ExtensionPackage`, `LanguageService`, `SolutionMonitor`, `GenerationController`)

5. **Buildalyzer** (git submodule) - MSBuild project file analyzer for resolving references.

### Generation Flow

1. User saves C# file in VS
2. `SolutionMonitor` detects change via solution events
3. `GenerationController` finds relevant .tst templates
4. `RoslynMetadataProvider` analyzes C# using Roslyn
5. Template engine executes template with code model
6. Generated TypeScript written to project

### Key Patterns

- **Metadata Provider Pattern:** Abstract interfaces in `Typewriter.Metadata`, concrete Roslyn implementation in `Typewriter.Metadata.Roslyn`
- **Implementation classes:** Named `*Impl.cs` in `src/Typewriter/CodeModel/`
- **Roslyn metadata:** Named `*Metadata.cs` in `src/Roslyn/`

## Template Syntax (.tst files)

- `$Classes(*Model)` - Filter classes by name pattern
- `$Properties[template][separator]` - Loop with separator
- `$Name`, `$Type`, `$Default` - Built-in accessors
- `${ ... }` - C# code blocks for custom logic

## Target Framework

- All main projects: .NET Framework 4.7.2 (VS extension requirement)
- Compatible with Visual Studio 2022 (v17.x) or above
