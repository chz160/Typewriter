# Source Tree Analysis

## Solution Overview

```
Typewriter.sln
├── Buildalyzer/                    # Git submodule (external)
├── src/CodeModel/                  # Public API
├── src/Metadata/                   # Provider interfaces
├── src/Roslyn/                     # Roslyn implementation
├── src/Typewriter/                 # Main extension
├── src/Tests/                      # Unit tests
└── src/ItemTemplates/              # VS item templates
```

---

## Project: Typewriter.CodeModel

**Path:** `src/CodeModel/`
**Assembly:** `Typewriter.CodeModel.dll`
**Target:** .NET Framework 4.7.2
**Dependencies:** None (pure library)

### Purpose

Defines the public API that template authors interact with. Contains abstract classes representing C# code elements.

### File Structure

```
src/CodeModel/
├── Typewriter.CodeModel.csproj
├── Item.cs                         # Base class for all model items
├── File.cs                         # Root container (abstract)
├── Class.cs                        # Class declaration (abstract)
├── Interface.cs                    # Interface declaration (abstract)
├── Record.cs                       # C# record (abstract)
├── Enum.cs                         # Enum declaration (abstract)
├── EnumValue.cs                    # Enum member (abstract)
├── Property.cs                     # Property (abstract)
├── Method.cs                       # Method (abstract)
├── Field.cs                        # Field (abstract)
├── Event.cs                        # Event (abstract)
├── Parameter.cs                    # Method parameter (abstract)
├── Attribute.cs                    # Applied attribute (abstract)
├── AttributeArgument.cs            # Attribute argument (abstract)
├── Delegate.cs                     # Delegate type (abstract)
├── Type.cs                         # Type reference (abstract)
├── TypeParameter.cs                # Generic type param (abstract)
├── Constant.cs                     # Const field (abstract)
├── StaticReadOnlyField.cs          # Static readonly field (abstract)
│
├── Collections/                    # Collection interfaces
│   ├── ItemCollection.cs           # Base collection
│   ├── IClassCollection.cs
│   ├── IPropertyCollection.cs
│   ├── IMethodCollection.cs
│   ├── IFieldCollection.cs
│   ├── IEnumCollection.cs
│   ├── IInterfaceCollection.cs
│   ├── IRecordCollection.cs
│   ├── IAttributeCollection.cs
│   ├── IParameterCollection.cs
│   └── ... (other collections)
│
├── Configuration/
│   └── Settings.cs                 # Base settings class
│
└── Extensions/
    ├── StringExtensions.cs         # String helpers
    ├── TypeExtensions.cs           # Type formatting
    └── WebApi/
        ├── HttpMethod.cs           # HTTP method helpers
        ├── RequestData.cs          # Request body info
        └── Url.cs                   # Route URL generation
```

### Key Types

| Type | Description |
|------|-------------|
| `Item` | Base with `Parent`, `Settings` properties |
| `File` | Contains Classes, Interfaces, Enums, Records, Delegates |
| `Class` | Properties, Methods, Fields, BaseClass, Attributes, NestedTypes |
| `Type` | Handles generics, arrays, nullability, collections |
| `Settings` | Configuration base (extended by SettingsImpl) |

---

## Project: Typewriter.Metadata

**Path:** `src/Metadata/`
**Assembly:** `Typewriter.Metadata.dll`
**Target:** .NET Framework 4.7.2
**Dependencies:** Typewriter.CodeModel (Settings type only)

### Purpose

Defines the contract interfaces for metadata providers. Technology-agnostic abstraction layer.

### File Structure

```
src/Metadata/
├── Typewriter.Metadata.csproj
│
├── Providers/
│   └── IMetadataProvider.cs        # Provider entry point
│
└── Interfaces/
    ├── INamedItem.cs               # Base: Name, FullName, AssemblyName
    ├── IFileMetadata.cs            # File container
    ├── IClassMetadata.cs           # Class with all members
    ├── IInterfaceMetadata.cs       # Interface
    ├── IRecordMetadata.cs          # C# record
    ├── IEnumMetadata.cs            # Enum
    ├── IEnumValueMetadata.cs       # Enum member
    ├── ITypeMetadata.cs            # Type reference (extends IClassMetadata)
    ├── IPropertyMetadata.cs        # Property
    ├── IMethodMetadata.cs          # Method
    ├── IFieldMetadata.cs           # Field base
    ├── IEventMetadata.cs           # Event
    ├── IParameterMetadata.cs       # Parameter
    ├── IAttributeMetadata.cs       # Attribute
    ├── IAttributeArgumentMetadata.cs # Attribute argument
    ├── IDelegateMetadata.cs        # Delegate
    ├── IConstantMetadata.cs        # Const field
    ├── IStaticReadOnlyFieldMetadata.cs # Static readonly
    └── ITypeParameterMetadata.cs   # Generic parameter
```

### Interface Count: 20

- 1 provider interface
- 19 metadata interfaces

---

## Project: Typewriter.Metadata.Roslyn

**Path:** `src/Roslyn/`
**Assembly:** `Typewriter.Metadata.Roslyn.dll`
**Target:** .NET Framework 4.7.2
**Dependencies:** Typewriter.Metadata, Microsoft.CodeAnalysis

### Purpose

Roslyn-based implementation of metadata interfaces. Extracts code structure from C# source using Roslyn semantic analysis.

### File Structure

```
src/Roslyn/
├── Typewriter.Metadata.Roslyn.csproj
├── RoslynMetadataProvider.cs       # IMetadataProvider implementation
├── RoslynFileMetadata.cs           # IFileMetadata
├── RoslynClassMetadata.cs          # IClassMetadata
├── RoslynInterfaceMetadata.cs      # IInterfaceMetadata
├── RoslynRecordMetadata.cs         # IRecordMetadata
├── RoslynEnumMetadata.cs           # IEnumMetadata
├── RoslynEnumValueMetadata.cs      # IEnumValueMetadata
├── RoslynTypeMetadata.cs           # ITypeMetadata
├── RoslynPropertyMetadata.cs       # IPropertyMetadata
├── RoslynMethodMetadata.cs         # IMethodMetadata
├── RoslynFieldMetadata.cs          # IFieldMetadata
├── RoslynEventMetadata.cs          # IEventMetadata
├── RoslynParameterMetadata.cs      # IParameterMetadata
├── RoslynAttributeMetadata.cs      # IAttributeMetadata
├── RoslynAttributeArgumentMetadata.cs
├── RoslynDelegateMetadata.cs       # IDelegateMetadata
├── RoslynConstantMetadata.cs       # IConstantMetadata
├── RoslynStaticReadOnlyFieldMetadata.cs
├── RoslynTypeParameterMetadata.cs
└── Extensions/
    └── SymbolExtensions.cs         # Roslyn helper methods
```

### VS Coupling

The `RoslynMetadataProvider` uses `VisualStudioWorkspace` from VS MEF services. This is the **primary VS dependency** in the metadata layer.

---

## Project: Typewriter (Main Extension)

**Path:** `src/Typewriter/`
**Assembly:** `Typewriter.dll`
**Target:** .NET Framework 4.7.2
**Dependencies:** All above + VS SDK

### Purpose

Main Visual Studio extension containing template engine, code model implementation, editor features, and VS integration.

### File Structure

```
src/Typewriter/
├── Typewriter.csproj
├── source.extension.vsixmanifest   # VSIX manifest
├── Constants.cs                    # Global constants
│
├── VisualStudio/                   # VS Integration (VS-Coupled)
│   ├── ExtensionPackage.cs         # AsyncPackage entry point
│   ├── LanguageService.cs          # .tst language registration
│   ├── Log.cs                      # Output window logger
│   ├── ErrorList.cs                # Error list integration
│   ├── TypewriterOptionsPage.cs    # Tools > Options page
│   ├── PathResolver.cs             # Project path resolution
│   └── ContextMenu/
│       └── RenderTemplate.cs       # Context menu commands
│
├── Generation/                     # Template Engine
│   ├── Template.cs                 # Main template processor
│   ├── Parser.cs                   # Template syntax parser (Reusable)
│   ├── SingleFileParser.cs         # Multi-file parser (Reusable)
│   ├── TemplateCodeParser.cs       # C# extraction (Reusable)
│   ├── Compiler.cs                 # Shadow class compiler
│   ├── ItemFilter.cs               # Collection filtering (Reusable)
│   └── Controllers/
│       ├── GenerationController.cs # Rendering orchestration
│       ├── TemplateController.cs   # Template management
│       ├── SolutionMonitor.cs      # File change detection
│       ├── EventQueue.cs           # Background processing
│       ├── SolutionExtensions.cs   # DTE extensions
│       └── SolutionFilesHelper.cs  # File enumeration
│
├── CodeModel/                      # Implementation Wrappers
│   ├── Implementation/
│   │   ├── FileImpl.cs             # File wrapper (Reusable)
│   │   ├── ClassImpl.cs            # Class wrapper (Reusable)
│   │   ├── InterfaceImpl.cs        # Interface wrapper
│   │   ├── RecordImpl.cs           # Record wrapper
│   │   ├── EnumImpl.cs             # Enum wrapper
│   │   ├── PropertyImpl.cs         # Property wrapper
│   │   ├── MethodImpl.cs           # Method wrapper
│   │   ├── FieldImpl.cs            # Field wrapper
│   │   ├── EventImpl.cs            # Event wrapper
│   │   ├── ParameterImpl.cs        # Parameter wrapper
│   │   ├── AttributeImpl.cs        # Attribute wrapper
│   │   ├── TypeImpl.cs             # Type wrapper
│   │   ├── DelegateImpl.cs         # Delegate wrapper
│   │   ├── ConstantImpl.cs         # Constant wrapper
│   │   ├── StaticReadOnlyFieldImpl.cs
│   │   ├── TypeParameterImpl.cs
│   │   ├── DocComment.cs           # XML doc comment parser
│   │   └── Helpers.cs              # Utility methods
│   │
│   ├── Collections/
│   │   ├── ClassCollectionImpl.cs
│   │   ├── PropertyCollectionImpl.cs
│   │   ├── MethodCollectionImpl.cs
│   │   ├── FieldCollectionImpl.cs
│   │   └── ... (other collections)
│   │
│   └── Configuration/
│       ├── SettingsImpl.cs         # Settings with DTE
│       └── ProjectHelpers.cs       # Project enumeration
│
└── TemplateEditor/                 # Editor Features
    ├── Editor.cs                   # Coordinator singleton
    │
    ├── Lexing/                     # Tokenization (Reusable)
    │   ├── TemplateLexer.cs        # .tst tokenizer
    │   ├── CodeLexer.cs            # C# code tokenizer
    │   ├── SemanticModel.cs        # Token storage
    │   ├── Contexts.cs             # Context regions
    │   ├── Identifiers.cs          # Identifier helpers
    │   ├── Tokens.cs               # Token types
    │   ├── Stream.cs               # Character stream
    │   ├── BraceStack.cs           # Brace matching
    │   └── Roslyn/
    │       ├── ShadowClass.cs      # Build C# class
    │       ├── ShadowWorkspace.cs  # Roslyn compilation
    │       └── Snippet.cs          # Code snippet
    │
    ├── Classifications/            # Syntax colors
    │   └── Classifications.cs      # Color definitions
    │
    └── Controllers/                # VS Editor APIs
        ├── ClassificationController.cs
        ├── CompletionController.cs
        ├── CompletionSource.cs
        ├── QuickInfoController.cs
        ├── BraceMatchingController.cs
        ├── OutliningController.cs
        ├── SyntaxErrorController.cs
        ├── SignatureHelpController.cs
        └── FormattingController.cs
```

---

## Project: Typewriter.Tests

**Path:** `src/Tests/`
**Target:** .NET Framework 4.7.2
**Dependencies:** xUnit, Should, NSubstitute

### File Structure

```
src/Tests/
├── Typewriter.Tests.csproj
├── CodeModel/
│   ├── Support/
│   │   └── MefHostingFixture.cs    # MEF test fixture
│   └── ... (test classes)
├── Metadata/
├── Roslyn/
└── Extensions/
```

---

## Project: Typewriter.ItemTemplates

**Path:** `src/ItemTemplates/`
**Target:** .NET Framework 4.7.2

### Purpose

Visual Studio item templates for creating new `.tst` files.

```
src/ItemTemplates/
├── Typewriter.ItemTemplates.csproj
└── TypeScriptTemplate/
    ├── TypeScriptTemplate.tst      # Default template content
    └── TypeScriptTemplate.vstemplate
```

---

## Critical Files for Understanding

### Must-Read Files (Priority Order)

1. **Entry Point**
   - `src/Typewriter/VisualStudio/ExtensionPackage.cs`

2. **Template Engine**
   - `src/Typewriter/Generation/Template.cs`
   - `src/Typewriter/Generation/Parser.cs`
   - `src/Typewriter/Generation/TemplateCodeParser.cs`

3. **Metadata Provider**
   - `src/Metadata/Providers/IMetadataProvider.cs`
   - `src/Roslyn/RoslynMetadataProvider.cs`

4. **Code Model**
   - `src/CodeModel/File.cs`
   - `src/CodeModel/Class.cs`
   - `src/Typewriter/CodeModel/Implementation/FileImpl.cs`
   - `src/Typewriter/CodeModel/Implementation/ClassImpl.cs`

5. **VS Integration**
   - `src/Typewriter/Generation/Controllers/GenerationController.cs`
   - `src/Typewriter/Generation/Controllers/SolutionMonitor.cs`

6. **Editor**
   - `src/Typewriter/TemplateEditor/Editor.cs`
   - `src/Typewriter/TemplateEditor/Lexing/TemplateLexer.cs`

---

## File Classification by Reusability

### Fully Reusable (No VS Dependencies)

```
src/CodeModel/**/*                          # All files
src/Metadata/**/*                           # All files
src/Roslyn/**/* (except provider)           # Roslyn*Metadata.cs files
src/Typewriter/Generation/Parser.cs
src/Typewriter/Generation/SingleFileParser.cs
src/Typewriter/Generation/TemplateCodeParser.cs
src/Typewriter/Generation/ItemFilter.cs
src/Typewriter/CodeModel/Implementation/**/*
src/Typewriter/CodeModel/Collections/**/*
src/Typewriter/TemplateEditor/Lexing/**/*
```

### Partially Reusable

```
src/Typewriter/Generation/Template.cs       # ~80% reusable
src/Typewriter/Generation/Compiler.cs       # ~50% reusable
src/Roslyn/RoslynMetadataProvider.cs        # Needs new constructor
```

### VS-Specific

```
src/Typewriter/VisualStudio/**/*
src/Typewriter/Generation/Controllers/**/*
src/Typewriter/TemplateEditor/Controllers/**/*
src/Typewriter/CodeModel/Configuration/SettingsImpl.cs
src/Typewriter/CodeModel/Configuration/ProjectHelpers.cs
```

---

## Lines of Code Estimate

| Project | Approx. LOC | Files |
|---------|-------------|-------|
| Typewriter.CodeModel | ~2,500 | ~35 |
| Typewriter.Metadata | ~800 | ~21 |
| Typewriter.Metadata.Roslyn | ~2,000 | ~22 |
| Typewriter (main) | ~8,000 | ~60 |
| Typewriter.Tests | ~1,500 | ~20 |
| **Total** | **~15,000** | **~158** |

---

## Namespace Structure

```
Typewriter.CodeModel
├── Typewriter.CodeModel.Collections
├── Typewriter.CodeModel.Configuration
└── Typewriter.CodeModel.Extensions

Typewriter.Metadata
├── Typewriter.Metadata.Interfaces
└── Typewriter.Metadata.Providers

Typewriter.Metadata.Roslyn
└── Typewriter.Metadata.Roslyn.Extensions

Typewriter
├── Typewriter.VisualStudio
├── Typewriter.VisualStudio.ContextMenu
├── Typewriter.Generation
├── Typewriter.Generation.Controllers
├── Typewriter.CodeModel.Implementation
├── Typewriter.CodeModel.Collections
├── Typewriter.CodeModel.Configuration
├── Typewriter.TemplateEditor
├── Typewriter.TemplateEditor.Lexing
├── Typewriter.TemplateEditor.Lexing.Roslyn
├── Typewriter.TemplateEditor.Classifications
└── Typewriter.TemplateEditor.Controllers
```
