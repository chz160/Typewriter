# Typewriter Project Overview

## Executive Summary

**Typewriter** is a Visual Studio extension (VSIX) that automatically generates TypeScript files from C# code using TypeScript Template (`.tst`) files. When developers save C# files, Typewriter watches for changes and regenerates corresponding TypeScript output based on template definitions.

**Repository:** Fork of [AdaskoTheBeAsT/Typewriter](https://github.com/AdaskoTheBeAsT/Typewriter), originally from [frhagn/Typewriter](https://github.com/frhagn/Typewriter)

**Primary Use Case:** Automatically generate TypeScript interfaces, classes, and models from C# DTOs, ViewModels, and API contracts to ensure type safety across the full stack.

---

## Technology Stack

| Category | Technology | Version | Notes |
|----------|------------|---------|-------|
| **Framework** | .NET Framework | 4.7.2 | Required for VS extension compatibility |
| **IDE Target** | Visual Studio | 2022+ (17.x, 18.x) | Supports amd64 and arm64 |
| **Code Analysis** | Roslyn (Microsoft.CodeAnalysis) | 4.14.0 | C# syntax/semantic analysis |
| **VS Integration** | Microsoft.VisualStudio.SDK | 17.14.40265 | VSIX extensibility APIs |
| **Language Services** | Microsoft.VisualStudio.LanguageServices | 4.14.0 | Roslyn/VS integration |
| **Build Analyzer** | Buildalyzer | (submodule) | MSBuild project analysis |
| **Testing** | xUnit + Should + NSubstitute | - | Unit test framework |

---

## Architecture Classification

**Type:** Monolithic Solution with Layered Architecture

**Architecture Pattern:** Provider Pattern + Abstract Factory

**Layers:**
1. **Metadata Abstraction** - Pure interfaces defining C# code structure
2. **Roslyn Provider** - Concrete metadata implementation using Roslyn
3. **Code Model** - User-facing wrapper over metadata
4. **Template Engine** - Parser, compiler, and rendering logic
5. **VS Integration** - Extension package, solution monitoring, editor features

---

## Repository Structure

```
Typewriter/
├── Buildalyzer/                    # Git submodule - MSBuild analyzer
├── src/
│   ├── CodeModel/                  # Public API: Abstract code model types
│   │   ├── Typewriter.CodeModel.csproj
│   │   ├── Class.cs, Property.cs, Method.cs, Enum.cs, etc.
│   │   ├── Collections/            # Collection interfaces (IClassCollection, etc.)
│   │   ├── Configuration/          # Settings base class
│   │   └── Extensions/             # WebApi, Type, String extensions
│   │
│   ├── Metadata/                   # Abstract interfaces for metadata providers
│   │   ├── Typewriter.Metadata.csproj
│   │   ├── Interfaces/             # 19 metadata interfaces
│   │   └── Providers/              # IMetadataProvider contract
│   │
│   ├── Roslyn/                     # Roslyn-based metadata implementation
│   │   ├── Typewriter.Metadata.Roslyn.csproj
│   │   ├── RoslynMetadataProvider.cs
│   │   └── Roslyn*Metadata.cs      # Concrete implementations
│   │
│   ├── Typewriter/                 # Main VS extension
│   │   ├── Typewriter.csproj
│   │   ├── VisualStudio/           # VS integration (package, services)
│   │   ├── Generation/             # Template engine
│   │   ├── CodeModel/              # Implementation wrappers
│   │   └── TemplateEditor/         # Editor features (IntelliSense, etc.)
│   │
│   ├── Tests/                      # xUnit test project
│   │   └── Typewriter.Tests.csproj
│   │
│   └── ItemTemplates/              # VS item templates for .tst files
│       └── Typewriter.ItemTemplates.csproj
│
├── Typewriter.sln                  # Solution file
├── Directory.Build.props           # Shared build configuration
└── CLAUDE.md                       # AI assistant instructions
```

---

## Core Concepts

### TypeScript Templates (.tst files)

Templates are text files with embedded expressions that access C# code model:

```tst
$Classes(*Model)[
export interface $Name {
    $Properties[
    $name: $Type;]
}]
```

**Syntax Elements:**
- `$Identifier` - Access property on code model
- `$Identifier(filter)` - Filter collections (wildcards, attributes, inheritance)
- `$Identifier[template]` - Render block for each item
- `$Identifier[template][separator]` - Render with separator between items
- `${...}` - Embedded C# code for custom logic
- `#reference "path.dll"` - Include external assemblies

### Provider Pattern

```
IMetadataProvider
    └─> GetFile(path)
         └─> IFileMetadata
              ├─> IClassMetadata[]
              ├─> IInterfaceMetadata[]
              ├─> IEnumMetadata[]
              ├─> IRecordMetadata[]
              └─> IDelegateMetadata[]
```

The metadata abstraction enables swapping the provider implementation without changing template logic.

### Code Model Layer

Templates interact with a high-level code model API:

```
File → Classes → Properties → Type
                 Methods   → Parameters → Type
                 Fields
                 Attributes
```

Each model class wraps a metadata interface and provides lazy-loaded collections.

---

## Key Features

| Feature | Description |
|---------|-------------|
| **Auto-generation** | Regenerates TypeScript when C# files change |
| **Filter expressions** | Wildcard (`*Model`), attribute (`[Required]`), inheritance (`:BaseClass`) |
| **Lambda filters** | Custom C# expressions: `$Classes(c => c.IsPublic)` |
| **Single-file mode** | Combine all output into one TypeScript file |
| **Custom extensions** | Embed C# code blocks for custom transformations |
| **IntelliSense** | Full editor support for .tst files |
| **C# 9+ support** | Records, file-scoped namespaces, init properties |
| **WebApi extensions** | Generate route URLs, HTTP methods from attributes |

---

## C# Language Support

| Feature | Supported Since |
|---------|-----------------|
| Classes, Interfaces, Enums | 1.0 |
| Generic types | 1.0 |
| Nullable reference types | 2.0 |
| Records (C# 9) | 2.20.0 |
| File-scoped namespaces (C# 10) | 2.26.0 |
| Value tuples | 2.0 |
| Static readonly fields | 2.0 |

---

## Dependencies Graph

```
Typewriter.CodeModel ←───────────────────┐
       ↑                                 │
       │ (Settings type)                 │
       │                                 │
Typewriter.Metadata ←───────────────┐    │
       ↑                            │    │
       │ (IMetadataProvider)        │    │
       │                            │    │
Typewriter.Metadata.Roslyn ←────────┤    │
       ↑                            │    │
       │ (Roslyn workspace)         │    │
       │                            │    │
Typewriter (main extension) ────────┴────┘
       │
       ├─> Microsoft.VisualStudio.SDK
       ├─> Microsoft.VisualStudio.LanguageServices
       └─> Buildalyzer (submodule)
```

---

## Version History Highlights

| Version | Key Changes |
|---------|-------------|
| 2.36.0 | VS 2026 support |
| 2.35.0 | VS 2025 support (ARM64) |
| 2.30.0 | TupleElement support, ElementType for arrays |
| 2.26.0 | File-scoped namespace support |
| 2.20.0 | C# 9 record support |
| 2.0.0 | Roslyn-based analysis (replaced Reflection) |
| 1.30.0 | Original baseline |

---

## Current Status

**Production Ready:** Yes - actively used for TypeScript generation from C# codebases

**VS Extension Only:** Currently requires Visual Studio; no CLI or CI/CD support

**Target Audience:** .NET developers who need to maintain TypeScript interfaces in sync with C# models

---

## Known Limitations

1. **VS Dependency** - Cannot run outside Visual Studio
2. **Synchronous Processing** - Large codebases may cause delays
3. **No Watch Mode CLI** - Cannot integrate into CI/CD pipelines
4. **Limited Error Recovery** - Template syntax errors can halt generation
5. **Single Solution Scope** - Cannot span multiple solutions

---

## Future Direction: CLI Support

The architecture's clean separation makes CLI support feasible:

- **Reusable (~60%):** Template engine, code model, lexing, Roslyn analysis
- **VS-Specific (~40%):** Solution monitoring, logging, error list, editor features

A CLI implementation would replace VS-specific components while reusing the core generation pipeline.
