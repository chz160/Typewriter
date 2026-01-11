# Typewriter Architecture

## Overview

Typewriter follows a **layered architecture** with clear separation of concerns. The design employs the **Provider Pattern** for metadata abstraction, enabling different code analysis implementations without affecting the template engine.

```
┌─────────────────────────────────────────────────────────────────┐
│                    Visual Studio Extension                       │
│  (ExtensionPackage, SolutionMonitor, TemplateEditor)            │
├─────────────────────────────────────────────────────────────────┤
│                     Template Engine                              │
│  (Template, Parser, Compiler, TemplateCodeParser)               │
├─────────────────────────────────────────────────────────────────┤
│                     Code Model Layer                             │
│  (FileImpl, ClassImpl, PropertyImpl, TypeImpl, ...)             │
├─────────────────────────────────────────────────────────────────┤
│                   Metadata Abstraction                           │
│  (IFileMetadata, IClassMetadata, IPropertyMetadata, ...)        │
├─────────────────────────────────────────────────────────────────┤
│                   Roslyn Provider                                │
│  (RoslynMetadataProvider, RoslynClassMetadata, ...)             │
└─────────────────────────────────────────────────────────────────┘
```

---

## Project Structure

### Assembly Dependencies

```
                    Typewriter.CodeModel
                           │
              ┌────────────┴────────────┐
              │                         │
              ▼                         ▼
    Typewriter.Metadata          (Settings type)
              │
              │ (IMetadataProvider)
              ▼
    Typewriter.Metadata.Roslyn
              │
              │ (Roslyn types)
              ▼
    Typewriter (main extension)
              │
              ├─▶ Microsoft.VisualStudio.SDK
              ├─▶ Microsoft.VisualStudio.LanguageServices
              └─▶ Buildalyzer (submodule)
```

### Project Responsibilities

| Project | Responsibility | VS Dependency |
|---------|---------------|---------------|
| `Typewriter.CodeModel` | Public API: abstract types for templates | None |
| `Typewriter.Metadata` | Provider contract interfaces | None |
| `Typewriter.Metadata.Roslyn` | Roslyn-based metadata extraction | VS Workspace only |
| `Typewriter` | Extension package, template engine, editor | Heavy |

---

## Layer 1: Metadata Abstraction

### Purpose

Define a technology-agnostic contract for representing C# code structure. This enables swapping Roslyn for alternative analyzers (Reflection, external API, cached data).

### Interface Hierarchy

```
INamedItem (Base)
├── IAttributeMetadata
├── IClassMetadata
│   └── ITypeMetadata (extends IClassMetadata)
├── IEnumMetadata
├── IEnumValueMetadata
├── IEventMetadata
├── IFieldMetadata
│   ├── IConstantMetadata
│   ├── IPropertyMetadata
│   ├── IStaticReadOnlyFieldMetadata
│   └── IMethodMetadata
│       └── IDelegateMetadata
├── IInterfaceMetadata
├── IParameterMetadata
└── IRecordMetadata

Standalone:
├── IFileMetadata (container)
├── IAttributeArgumentMetadata
└── ITypeParameterMetadata
```

### Provider Entry Point

```csharp
// src/Metadata/Providers/IMetadataProvider.cs
public interface IMetadataProvider
{
    IFileMetadata GetFile(string path, Settings settings, Action<string[]> requestRender);
}
```

**Parameters:**
- `path` - Absolute path to C# source file
- `settings` - Configuration from template settings block
- `requestRender` - Callback for partial class coordination

### Key Interfaces

| Interface | Purpose | Key Properties |
|-----------|---------|----------------|
| `IFileMetadata` | Root container | Classes, Interfaces, Enums, Records, Delegates |
| `IClassMetadata` | Class declaration | Properties, Methods, Fields, BaseClass, Attributes |
| `ITypeMetadata` | Type reference | IsNullable, IsDictionary, IsEnumerable, DefaultValue |
| `IPropertyMetadata` | Property | HasGetter, HasSetter, IsAbstract, IsVirtual |
| `IMethodMetadata` | Method | Parameters, TypeParameters, IsGeneric |

---

## Layer 2: Roslyn Provider

### Implementation

```csharp
// src/Roslyn/RoslynMetadataProvider.cs
public class RoslynMetadataProvider : IMetadataProvider
{
    private readonly Workspace _workspace;

    public RoslynMetadataProvider()
    {
        // Gets VS workspace via MEF
        var componentModel = ServiceProvider.GlobalProvider
            .GetService(typeof(SComponentModel)) as IComponentModel;
        _workspace = componentModel?.GetService<VisualStudioWorkspace>();
    }

    public IFileMetadata GetFile(string path, Settings settings, Action<string[]> requestRender)
    {
        var docId = _workspace.CurrentSolution
            .GetDocumentIdsWithFilePath(path).FirstOrDefault();
        if (docId != null)
        {
            var document = _workspace.CurrentSolution.GetDocument(docId);
            return new RoslynFileMetadata(document, settings, requestRender);
        }
        return null;
    }
}
```

### VS Coupling Point

The `VisualStudioWorkspace` is the **critical VS dependency**. For CLI support, this would be replaced with a standalone Roslyn workspace:

```csharp
// Hypothetical CLI implementation
public class StandaloneRoslynProvider : IMetadataProvider
{
    private readonly Workspace _workspace;

    public StandaloneRoslynProvider(string solutionPath)
    {
        _workspace = CreateStandaloneWorkspace();
        _workspace.OpenSolutionAsync(solutionPath).Wait();
    }
}
```

---

## Layer 3: Code Model

### Purpose

Provide a user-friendly API for template authors. Wraps metadata interfaces with lazy-loaded collections and convenience methods.

### Pattern: Wrapper + Factory

```csharp
// src/Typewriter/CodeModel/Implementation/ClassImpl.cs
public sealed class ClassImpl : Class
{
    private readonly IClassMetadata _metadata;
    private readonly Settings _settings;

    // Direct property delegation
    public override string Name => _metadata.Name.TrimStart('@');
    public override bool IsAbstract => _metadata.IsAbstract;

    // Lazy-loaded collection
    private IPropertyCollection _properties;
    public override IPropertyCollection Properties =>
        _properties ?? (_properties = PropertyImpl.FromMetadata(
            _metadata.Properties, this, Settings));

    // Static factory
    public static IClassCollection FromMetadata(
        IEnumerable<IClassMetadata> metadata, Item parent, Settings settings)
    {
        return new ClassCollectionImpl(
            metadata.Select(c => new ClassImpl(c, parent, settings)));
    }
}
```

### Collection Types

Each collection implements filtering capabilities:

```csharp
public interface IClassCollection : IEnumerable<Class>
{
    IClassCollection Where(Func<Class, bool> predicate);
    // ... filter methods
}
```

Filter types:
- **Name filter:** `$Classes(*Model)` - Wildcard matching
- **Attribute filter:** `$Properties([Required])` - Filter by attribute
- **Inheritance filter:** `$Classes(:BaseClass)` - Filter by base type
- **Lambda filter:** `$Classes(c => c.IsPublic)` - Custom predicate

---

## Layer 4: Template Engine

### Components

```
Template Processing Pipeline
│
├── TemplateCodeParser ─── Extract C# code blocks
│   └── ShadowClass ────── Build compilable C# class
│       └── ShadowWorkspace ── Roslyn compilation
│
├── Parser ─────────────── Template syntax parsing
│   └── ItemFilter ─────── Collection filtering
│
├── SingleFileParser ───── Multi-file mode
│
├── Compiler ───────────── Compile shadow class
│
└── Template ───────────── Orchestrate rendering
```

### Template Syntax Processing

```csharp
// src/Typewriter/Generation/Parser.cs
public static string Parse(string template, object context, ...)
{
    // Scan for $ identifiers
    // Extract filter expressions
    // Use reflection to get values
    // Recursively process nested templates
}
```

**Supported Syntax:**

| Pattern | Meaning |
|---------|---------|
| `$Name` | Property access |
| `$Classes(*)` | Collection with wildcard filter |
| `$Properties[template]` | Block rendering |
| `$Properties[template][separator]` | Block with separator |
| `$IsAbstract[true][false]` | Boolean conditional |
| `${...}` | C# code block |
| `#reference "path.dll"` | External assembly |

### Shadow Class Compilation

Templates can contain C# code that gets compiled at runtime:

```csharp
// User template
${
    string ToCamelCase(string s) => char.ToLower(s[0]) + s.Substring(1);
}
$Properties[$ToCamelCase($Name): $Type]

// Generated shadow class
namespace __Typewriter
{
    using System;
    using Typewriter.CodeModel;

    public class Template
    {
        public string ToCamelCase(string s) => char.ToLower(s[0]) + s.Substring(1);
    }
}
```

---

## Layer 5: VS Integration

### Extension Package

```csharp
// src/Typewriter/VisualStudio/ExtensionPackage.cs
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
public sealed class ExtensionPackage : AsyncPackage
{
    // Initialization
    // Event wiring
    // Controller creation
}
```

### File Change Detection

```
User saves C# file
        │
        ▼
SolutionMonitor (IVsRunningDocTableEvents3)
        │
        ▼
CsFileChanged event
        │
        ▼
GenerationController.OnCsFileChanged()
        │
        ▼
Delay 1000ms (Roslyn workspace refresh)
        │
        ▼
EventQueue.Enqueue(RenderingAction)
        │
        ▼
IMetadataProvider.GetFile()
        │
        ▼
Template.RenderFile()
        │
        ▼
Write TypeScript file
```

### Editor Features

| Feature | Implementation |
|---------|---------------|
| Syntax highlighting | ClassificationController + TemplateLexer |
| IntelliSense | CompletionController + CodeLexer |
| Error squiggles | SyntaxErrorController + ShadowWorkspace |
| Brace matching | BraceMatchingController |
| Code folding | OutliningController |
| Quick info | QuickInfoController |

---

## Component Reusability Analysis

### Fully Reusable (CLI-Ready)

**Generation/**
- `Parser.cs` - Template parsing engine
- `SingleFileParser.cs` - Multi-file parser
- `TemplateCodeParser.cs` - C# code extraction
- `ItemFilter.cs` - Collection filtering
- ~50% of `Compiler.cs` (Roslyn compilation)

**CodeModel/Implementation/**
- All `*Impl.cs` files
- All collection implementations
- Helpers and utilities

**TemplateEditor/Lexing/**
- `TemplateLexer.cs` - Token analysis
- `CodeLexer.cs` - C# code analysis
- `SemanticModel.cs` - Token storage
- `ShadowClass.cs` - Roslyn integration
- `ShadowWorkspace.cs` - Compilation

**Reusability: ~60% of codebase**

### VS-Coupled (Requires Replacement)

**VisualStudio/**
- `ExtensionPackage.cs` - Package initialization
- `LanguageService.cs` - .tst file registration
- `Log.cs` - Output window logger
- `ErrorList.cs` - Error list integration
- `TypewriterOptionsPage.cs` - Options dialog
- `PathResolver.cs` - Project-relative paths

**Generation/Controllers/**
- `SolutionMonitor.cs` - VS solution events
- `GenerationController.cs` - DTE/metadata orchestration
- `TemplateController.cs` - DTE-based template discovery
- `EventQueue.cs` - Status bar integration

**TemplateEditor/Controllers/**
- All controllers (VS editor APIs)

**VS-Coupled: ~40% of codebase**

---

## CLI Extraction Strategy

### Components to Replace

| VS Component | CLI Replacement |
|--------------|-----------------|
| `SolutionMonitor` | `FileSystemWatcher` |
| `TemplateController` | Glob pattern discovery |
| `GenerationController` | CLI orchestrator |
| `Log` | Console/file logger |
| `ErrorList` | Console error output |
| `SettingsImpl` | File-based settings |
| `PathResolver` | Relative path resolver |
| `VisualStudioWorkspace` | Standalone Roslyn workspace |

### Proposed CLI Architecture

```
TypewriterCLI/
├── Program.cs              # Entry point, argument parsing
├── FileWatcher.cs          # FileSystemWatcher wrapper
├── TemplateFinder.cs       # Glob-based .tst discovery
├── ProjectAnalyzer.cs      # Standalone workspace integration
├── ConsoleLogger.cs        # ILogger implementation
├── CliSettings.cs          # Without DTE dependencies
├── CliPathResolver.cs      # Relative path resolution
├── RenderingEngine.cs      # Generation orchestration
│
└── [Reference existing assemblies]
    ├── Typewriter.CodeModel
    ├── Typewriter.Metadata
    └── Typewriter.Metadata.Roslyn (with new provider)
```

### CLI Provider Implementation

```csharp
public class StandaloneMetadataProvider : IMetadataProvider
{
    private readonly Workspace _workspace;
    private readonly Solution _solution;

    public StandaloneMetadataProvider(string solutionPath)
    {
        _workspace = CreateStandaloneWorkspace();
        _solution = _workspace.OpenSolutionAsync(solutionPath)
            .GetAwaiter().GetResult();
    }

    public IFileMetadata GetFile(string path, Settings settings,
        Action<string[]> requestRender)
    {
        var docId = _solution.GetDocumentIdsWithFilePath(path)
            .FirstOrDefault();
        if (docId != null)
        {
            var document = _solution.GetDocument(docId);
            return new RoslynFileMetadata(document, settings, requestRender);
        }
        return null;
    }
}
```

### Minimal Changes Required

1. **New project:** `Typewriter.CLI` - Console application
2. **Modify:** `RoslynMetadataProvider` to accept workspace via constructor
3. **Extract:** Reusable components into shared assembly (optional)
4. **Add:** CLI argument parsing (solution path, templates, watch mode)
5. **Add:** Configuration file support (`.typewriterrc.json`)

---

## Data Flow Diagram

### Template Rendering Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                         Input                                    │
│  C# Source File ─────────────────────────────────────────────▶  │
└────────────────────────────────────┬────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Roslyn Analysis                                │
│  Document ──▶ SemanticModel ──▶ INamedTypeSymbol                │
└────────────────────────────────────┬────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Metadata Extraction                            │
│  INamedTypeSymbol ──▶ RoslynClassMetadata ──▶ IClassMetadata    │
└────────────────────────────────────┬────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Code Model Wrapping                            │
│  IClassMetadata ──▶ ClassImpl ──▶ Class (public API)            │
└────────────────────────────────────┬────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Template Processing                            │
│  Template + Class ──▶ Parser ──▶ TypeScript string              │
└────────────────────────────────────┬────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Output                                   │
│  ◀─────────────────────────────────────────── TypeScript File   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Design Patterns

| Pattern | Usage |
|---------|-------|
| **Provider** | `IMetadataProvider` decouples analysis from engine |
| **Abstract Factory** | `FromMetadata()` static methods |
| **Lazy Initialization** | Code model collections |
| **Singleton** | `Editor.Instance`, `Log.Instance` |
| **Observer** | File change events |
| **Template Method** | Parser template processing |
| **Facade** | `Editor` coordinates editor features |
| **Command** | Context menu handlers |

---

## Key Design Decisions

### 1. Metadata Abstraction

**Decision:** Separate interfaces from Roslyn implementation

**Rationale:** Enables alternative providers, testability, and CLI support

**Trade-off:** Additional indirection, but enables flexibility

### 2. Lazy Loading

**Decision:** Collections load on first access

**Rationale:** Memory efficiency for large codebases

**Trade-off:** First access may be slower

### 3. Reflection-Based Parsing

**Decision:** Use reflection to access code model properties

**Rationale:** Simple template syntax, no code generation needed

**Trade-off:** Runtime overhead, less compile-time safety

### 4. Shadow Class Compilation

**Decision:** Compile custom code at runtime

**Rationale:** Enables powerful custom extensions

**Trade-off:** Complexity, potential security considerations

---

## Architectural Strengths

1. **Clean Separation** - Each layer has clear responsibility
2. **Provider Pattern** - Swappable metadata sources
3. **Lazy Loading** - Efficient memory usage
4. **Extensibility** - Custom C# code in templates
5. **Type-Safe Model** - Strongly-typed code representation

## Areas for Improvement

1. **VS Coupling** - ~40% of code is VS-specific
2. **Synchronous I/O** - Could benefit from async patterns
3. **Global State** - Several singletons complicate testing
4. **Hard-Coded Delays** - 1000ms Roslyn refresh delay is fragile
5. **Limited Caching** - Metadata could be cached more aggressively
