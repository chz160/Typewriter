# Typewriter Documentation Index

## Overview

**Typewriter** is a Visual Studio extension that generates TypeScript files from C# code using TypeScript Template (`.tst`) files. This documentation provides comprehensive technical details for developers working on the codebase.

---

## Documentation Contents

### [Project Overview](./project-overview.md)
Executive summary, technology stack, architecture classification, core concepts, and feature list.

### [Architecture](./architecture.md)
Detailed architectural analysis including:
- Layered architecture diagram
- Provider pattern implementation
- Metadata abstraction layer
- Code model design
- Template engine components
- VS integration details
- **CLI extraction strategy** (key for PRD development)
- Reusability analysis (~60% reusable, ~40% VS-specific)

### [Source Tree Analysis](./source-tree-analysis.md)
Complete file-by-file breakdown of:
- All projects and their responsibilities
- File structure with annotations
- Critical files for understanding
- Reusability classification per file
- Namespace structure

### [Development Guide](./development-guide.md)
Practical development information:
- Build commands
- Test commands
- Debugging tips
- Code conventions
- Adding new features
- Troubleshooting

---

## Quick Reference

### Build

```bash
# Build
MSBuild.exe Typewriter.sln /p:Configuration=Release

# Test
dotnet test src/Tests/Typewriter.Tests.csproj

# Clean
pwsh ./clean.ps1
```

### Key Technologies

| Technology | Version |
|------------|---------|
| .NET Framework | 4.7.2 |
| Visual Studio | 2022+ |
| Roslyn | 4.14.0 |
| VS SDK | 17.14 |

### Project Layout

```
src/
├── CodeModel/      # Public API
├── Metadata/       # Provider interfaces
├── Roslyn/         # Roslyn implementation
├── Typewriter/     # VS extension
└── Tests/          # Unit tests
```

---

## For PRD Development

The following documentation sections are particularly relevant for creating a PRD to extend Typewriter with CLI functionality:

1. **[Architecture > CLI Extraction Strategy](./architecture.md#cli-extraction-strategy)**
   - Components that need replacement
   - Proposed CLI architecture
   - MSBuild workspace provider implementation

2. **[Architecture > Component Reusability](./architecture.md#component-reusability-analysis)**
   - 60% reusable code identification
   - VS-coupled components requiring replacement

3. **[Source Tree > File Classification](./source-tree-analysis.md#file-classification-by-reusability)**
   - Per-file reusability status
   - Concrete list of files to reuse vs replace

### Key Insights for CLI

- **Provider Pattern enables swappability**: Replace `VisualStudioWorkspace` with `MSBuildWorkspace`
- **Template engine is VS-agnostic**: `Parser.cs`, `TemplateCodeParser.cs` fully reusable
- **Code model is reusable**: All `*Impl.cs` files have no VS dependencies
- **Main coupling points**:
  - `RoslynMetadataProvider` (workspace injection)
  - `SettingsImpl` (DTE project enumeration)
  - `GenerationController` (solution monitoring)

---

## Document Metadata

| Field | Value |
|-------|-------|
| Generated | 2026-01-10 |
| Project | Typewriter |
| Type | Brownfield .NET 4.7.2 |
| Purpose | PRD preparation for CLI extension |

---

## Related Resources

- [CLAUDE.md](../../CLAUDE.md) - AI assistant instructions
- [README.md](../../README.md) - Project readme
- [Typewriter.sln](../../Typewriter.sln) - Solution file
