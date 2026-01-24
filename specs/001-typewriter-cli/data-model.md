# Data Model: Typewriter CLI Extension

**Feature Branch**: `001-typewriter-cli`
**Date**: 2026-01-11

## Overview

The Typewriter CLI operates on file-based inputs and outputs with no persistent storage layer. This document defines the conceptual data models used during CLI execution.

## Core Entities

### 1. CLI Settings

Represents configuration for CLI execution, sourced from command-line arguments and optional config files.

```
CliSettings
├── SolutionPath: string?        # Path to .sln file (mutually exclusive with ProjectPath)
├── ProjectPath: string?         # Path to .csproj file (mutually exclusive with SolutionPath)
├── ConfigPath: string?          # Explicit config file path (optional)
├── TemplatePatterns: string[]   # Glob patterns for .tst files (default: ["**/*.tst"])
├── ExcludePatterns: string[]    # Glob patterns to exclude (default: ["**/obj/**", "**/bin/**"])
├── Verbosity: VerbosityLevel    # Output verbosity (Quiet, Normal, Verbose)
├── DryRun: bool                 # Preview without writing files
├── JsonOutput: bool             # Output in JSON format
└── WorkingDirectory: string     # Base directory for relative paths
```

**Validation Rules**:
- Either `SolutionPath` OR `ProjectPath` must be specified (not both)
- Paths must exist and be readable
- Solution must be valid MSBuild format

### 2. Configuration File

JSON schema for `.typewriterrc` or `typewriter.json` files.

```json
{
  "$schema": "typewriter-config-schema.json",
  "solution": "./MyProject.sln",
  "project": null,
  "templates": ["**/*.tst"],
  "exclude": ["**/obj/**", "**/bin/**"],
  "verbosity": "normal"
}
```

**Field Definitions**:
| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| solution | string | No* | null | Relative path to .sln file |
| project | string | No* | null | Relative path to .csproj file |
| templates | string[] | No | ["**/*.tst"] | Glob patterns for templates |
| exclude | string[] | No | ["**/obj/**", "**/bin/**"] | Patterns to exclude |
| verbosity | enum | No | "normal" | quiet, normal, verbose |

*Either `solution` OR `project` must be specified if no CLI args provided

### 3. Template File (.tst)

Represents a Typewriter template to be processed.

```
TemplateFile
├── Path: string                 # Absolute path to .tst file
├── RelativePath: string         # Path relative to solution root
├── Content: string              # Raw template content
├── ReferencedFiles: string[]    # C# files referenced by template
├── OutputPath: string?          # Derived output path for generated .ts
└── IsValid: bool                # Template compiles successfully
```

**Discovery Rules**:
- Templates are discovered via glob patterns from solution root
- Default pattern: `**/*.tst`
- Exclude patterns filter out obj/bin directories

### 4. Generation Context

Represents the state of a single generation run.

```
GenerationContext
├── Settings: CliSettings
├── StartTime: DateTime
├── Templates: TemplateFile[]
├── Results: GenerationResult[]
├── Warnings: DiagnosticMessage[]
├── Errors: DiagnosticMessage[]
└── Duration: TimeSpan
```

### 5. Generation Result

Represents the outcome of processing a single template.

```
GenerationResult
├── Template: TemplateFile
├── OutputFiles: OutputFile[]
├── Success: bool
├── Errors: DiagnosticMessage[]
└── Warnings: DiagnosticMessage[]
```

### 6. Output File

Represents a generated TypeScript file.

```
OutputFile
├── Path: string                 # Absolute path to generated .ts file
├── RelativePath: string         # Path relative to solution root
├── Content: string              # Generated TypeScript content
├── SourceTemplate: TemplateFile
├── WasWritten: bool             # False if dry-run mode
└── ByteCount: int               # Size of generated content
```

### 7. Diagnostic Message

Represents an error or warning during generation.

```
DiagnosticMessage
├── Severity: DiagnosticSeverity # Error, Warning, Info
├── Message: string              # Human-readable message
├── FilePath: string?            # Source file (if applicable)
├── Line: int?                   # Line number (if applicable)
├── Column: int?                 # Column number (if applicable)
└── Code: string?                # Diagnostic code (e.g., TW001)
```

**Format**: `{FilePath}:{Line}:{Column}: {Severity}: {Message}`
**Example**: `CustomerModel.tst:15:8: error: Cannot resolve type 'OrderStatus'`

## Entity Relationships

```
┌─────────────────┐
│   CliSettings   │
└────────┬────────┘
         │ configures
         ▼
┌─────────────────┐
│GenerationContext│
└────────┬────────┘
         │ contains
         ▼
┌─────────────────┐     ┌─────────────────┐
│  TemplateFile   │────▶│ GenerationResult│
└─────────────────┘     └────────┬────────┘
                                 │ produces
                                 ▼
                        ┌─────────────────┐
                        │   OutputFile    │
                        └─────────────────┘
```

## State Transitions

### Template Processing States

```
[Discovered] → [Parsed] → [Validated] → [Generated] → [Written]
     │              │           │             │
     └──────────────┴───────────┴─────────────┘
                          │
                          ▼
                      [Failed]
```

| State | Description | Can Proceed |
|-------|-------------|-------------|
| Discovered | Template file found via glob | Yes |
| Parsed | Template content loaded and parsed | Yes |
| Validated | Template compiles without errors | Yes |
| Generated | TypeScript content produced | Yes |
| Written | Output file written to disk | Terminal |
| Failed | Error during any stage | Terminal |

### CLI Execution States

```
[Initialize] → [Discover] → [Process] → [Report] → [Exit]
      │              │           │          │
      └──────────────┴───────────┴──────────┘
                          │
                          ▼
                      [Error]
```

## Enumerations

### VerbosityLevel

```
enum VerbosityLevel {
    Quiet,      // Errors only (stderr)
    Normal,     // Summary output (default)
    Verbose     // File-by-file details
}
```

### DiagnosticSeverity

```
enum DiagnosticSeverity {
    Info,       // Informational (verbose mode only)
    Warning,    // Non-blocking issue
    Error       // Blocking failure
}
```

### ExitCode

```
enum ExitCode {
    Success = 0,              // All files generated
    GenerationFailure = 1,    // Template/compilation errors
    InvalidArguments = 2      // Bad CLI args or config
}
```

## Data Flow

```
CLI Arguments / Config File
          │
          ▼
    ┌─────────────┐
    │ CliSettings │
    └──────┬──────┘
           │
           ▼
    ┌─────────────────┐
    │ TemplateFinder  │ ────▶ .tst files
    └────────┬────────┘
             │
             ▼
    ┌─────────────────┐
    │CliMetadataProvider│ ────▶ Solution/Project
    └────────┬────────┘            │
             │                     ▼
             │              AdhocWorkspace
             │                     │
             ▼                     ▼
    ┌─────────────────┐     ┌─────────────┐
    │  Template.Render │◀───│IFileMetadata│
    └────────┬────────┘     └─────────────┘
             │
             ▼
    ┌─────────────────┐
    │   OutputFile    │ ────▶ .ts files
    └────────┬────────┘
             │
             ▼
    ┌─────────────────┐
    │  ConsoleOutput  │ ────▶ stdout/stderr
    └─────────────────┘
```

## File System Interactions

### Input Files

| Type | Pattern | Required |
|------|---------|----------|
| Solution | *.sln | Yes* |
| Project | *.csproj | Yes* |
| Template | **/*.tst | Yes |
| C# Source | **/*.cs | Yes |

*Either solution OR project required

### Output Files

| Type | Location | Creation |
|------|----------|----------|
| TypeScript | Per template output directive | Always (unless dry-run) |

### Config Files

| Name | Location | Discovery Order |
|------|----------|-----------------|
| .typewriterrc | Current directory | 1st |
| typewriter.json | Current directory | 2nd |
| .typewriterrc | Solution root | 3rd |
| typewriter.json | Solution root | 4th |
