# Development Guide

## Prerequisites

- **Visual Studio 2022** (17.x) or later
- **.NET Framework 4.7.2 SDK**
- **VS Extensibility Workload** installed in VS
- **Git** for submodule management

---

## Getting Started

### Clone with Submodules

```bash
git clone --recurse-submodules https://github.com/[repo]/Typewriter.git
```

Or if already cloned:

```bash
./submodules-sync.bat
```

### Build Commands

```bash
# Build solution (requires VS 2022 MSBuild)
"/mnt/c/Program Files/Microsoft Visual Studio/18/Enterprise/MSBuild/Current/Bin/amd64/MSBuild.exe" Typewriter.sln /p:Configuration=Release

# Clean build artifacts
pwsh ./clean.ps1

# Package VSIX
pwsh ./build/PackageReferences.ps1
```

**Output:** `src/Typewriter/bin/Release/Typewriter.vsix`

### Run Tests

```bash
# Run all tests
dotnet test src/Tests/Typewriter.Tests.csproj

# Run specific test class
dotnet test src/Tests/Typewriter.Tests.csproj --filter "FullyQualifiedName~ClassTests"

# Run single test
dotnet test src/Tests/Typewriter.Tests.csproj --filter "FullyQualifiedName~ClassTests.Expect_name_to_match_class_name"

# Using vstest.console (alternative)
"/mnt/c/Program Files/Microsoft Visual Studio/18/Enterprise/Common7/IDE/Extensions/TestPlatform/vstest.console.exe" src/Tests/bin/Debug/Typewriter.Tests.dll
```

---

## Project Structure Quick Reference

```
src/
├── CodeModel/      # Public API (abstract classes)
├── Metadata/       # Provider interfaces
├── Roslyn/         # Roslyn implementation
├── Typewriter/     # VS extension (main)
├── Tests/          # Unit tests
└── ItemTemplates/  # VS item templates
```

---

## Development Workflow

### 1. Working on Template Engine

**Files:** `src/Typewriter/Generation/`

- `Parser.cs` - Template syntax processing
- `SingleFileParser.cs` - Multi-file mode
- `TemplateCodeParser.cs` - C# code extraction
- `Compiler.cs` - Shadow class compilation

**Testing:** These components are largely independent of VS and can be unit tested.

### 2. Working on Code Model

**Files:**
- Public API: `src/CodeModel/`
- Implementation: `src/Typewriter/CodeModel/Implementation/`

**Pattern:**
1. Add/modify abstract class in `src/CodeModel/`
2. Add/modify implementation in `src/Typewriter/CodeModel/Implementation/`
3. Add corresponding metadata interface if needed (`src/Metadata/`)
4. Implement metadata in Roslyn project (`src/Roslyn/`)

### 3. Working on Metadata Provider

**Files:** `src/Roslyn/`

**Pattern:**
1. Implement `I*Metadata` interface
2. Use Roslyn's `ISymbol` types to extract data
3. Consider lazy loading for expensive operations

### 4. Working on VS Integration

**Files:** `src/Typewriter/VisualStudio/`, `src/Typewriter/Generation/Controllers/`

**Debugging:**
1. Set `src/Typewriter` as startup project
2. Press F5 to launch VS Experimental Instance
3. Open a solution with .tst files
4. Set breakpoints in extension code

---

## Adding New Features

### Adding a New Code Model Property

**Example:** Adding `IsSealed` to Class

1. **Update abstract class:**
```csharp
// src/CodeModel/Class.cs
public abstract class Class : Item
{
    public abstract bool IsSealed { get; }
    // ... existing properties
}
```

2. **Update metadata interface:**
```csharp
// src/Metadata/Interfaces/IClassMetadata.cs
public interface IClassMetadata : INamedItem
{
    bool IsSealed { get; }
    // ... existing properties
}
```

3. **Update Roslyn implementation:**
```csharp
// src/Roslyn/RoslynClassMetadata.cs
public class RoslynClassMetadata : IClassMetadata
{
    private readonly INamedTypeSymbol _symbol;

    public bool IsSealed => _symbol.IsSealed;
    // ... existing properties
}
```

4. **Update wrapper implementation:**
```csharp
// src/Typewriter/CodeModel/Implementation/ClassImpl.cs
public sealed class ClassImpl : Class
{
    private readonly IClassMetadata _metadata;

    public override bool IsSealed => _metadata.IsSealed;
    // ... existing properties
}
```

5. **Add tests:**
```csharp
// src/Tests/CodeModel/ClassTests.cs
[Fact]
public void Expect_IsSealed_to_be_true_for_sealed_class()
{
    // Arrange, Act, Assert
}
```

### Adding a New Template Filter

**Example:** Adding `[Sealed]` attribute filter

1. **Update ItemFilter.cs:**
```csharp
// src/Typewriter/Generation/ItemFilter.cs
public static class ItemFilter
{
    // Add new filter pattern recognition
    // Update ApplyFilter method
}
```

### Adding New Editor Feature

1. Create controller implementing VS editor interface
2. Register in `Editor.cs` or via MEF export
3. Implement feature logic using `Editor` services

---

## Debugging Tips

### VS Extension Debugging

1. Open `Typewriter.sln` in VS
2. Set `Typewriter` project as startup
3. Go to Project Properties > Debug
4. Ensure "Start external program" points to devenv.exe
5. Command line arguments: `/rootsuffix Exp`
6. F5 launches VS Experimental Instance

### Template Rendering Issues

1. Add breakpoint in `GenerationController.OnCsFileChanged()`
2. Trace through `Template.RenderFile()`
3. Check `Parser.Parse()` for template processing

### Roslyn Metadata Issues

1. Add breakpoint in `RoslynMetadataProvider.GetFile()`
2. Inspect `INamedTypeSymbol` properties
3. Check workspace document resolution

### Logging

```csharp
// Use built-in logging
Log.Debug("Debug message");
Log.Info("Info message");
Log.Warn("Warning message");
Log.Error("Error message");
```

Logs appear in VS Output Window > "Typewriter" pane.

---

## Code Conventions

### Naming

- `*Impl.cs` - Implementation wrapper classes
- `*Metadata.cs` - Metadata interfaces/implementations
- `*Controller.cs` - VS editor feature controllers
- `I*Collection.cs` - Collection interfaces

### Patterns

- **Lazy Loading:** Use `_field ?? (_field = ...)` pattern for collections
- **Static Factories:** `FromMetadata()` methods create instances from metadata
- **Settings Access:** Pass `Settings` through constructors

### Null Handling

```csharp
// Prefer null-coalescing
public string Name => _metadata.Name ?? string.Empty;

// Use nullable for optional properties
public IClassMetadata BaseClass => _metadata.BaseClass;
```

---

## Testing

### Test Framework

- **xUnit** - Test runner
- **Should** - Fluent assertions
- **NSubstitute** - Mocking

### Test Fixture

```csharp
public class MyTests : IClassFixture<MefHostingFixture>
{
    private readonly MefHostingFixture _fixture;

    public MyTests(MefHostingFixture fixture)
    {
        _fixture = fixture;
    }
}
```

### Writing Tests

```csharp
[Fact]
public void Expect_property_to_have_correct_value()
{
    // Arrange
    var metadata = Substitute.For<IClassMetadata>();
    metadata.Name.Returns("TestClass");

    // Act
    var classImpl = new ClassImpl(metadata, null, null);

    // Assert
    classImpl.Name.ShouldBe("TestClass");
}
```

---

## Common Tasks

### Regenerate VSIX

```bash
# Clean
pwsh ./clean.ps1

# Build Release
MSBuild.exe Typewriter.sln /p:Configuration=Release

# Output at: src/Typewriter/bin/Release/Typewriter.vsix
```

### Update Buildalyzer Submodule

```bash
cd Buildalyzer
git fetch origin
git checkout <new-version>
cd ..
git add Buildalyzer
git commit -m "Update Buildalyzer to <version>"
```

### Test Template Syntax

1. Open VS with extension loaded
2. Create new .tst file
3. Add template content
4. Save to trigger rendering
5. Check Output Window for errors

---

## Troubleshooting

### Build Errors

**"Microsoft.VSSDK.BuildTools not found"**
- Run `dotnet restore` then build again
- Ensure NuGet package sources configured

**"Roslyn version mismatch"**
- Check `Microsoft.CodeAnalysis.*` versions in all projects
- Align with VS SDK requirements

### Runtime Errors

**"Workspace is null"**
- Extension may not be fully initialized
- Check MEF composition in VS Experimental

**"Document not found"**
- File may not be in solution/project
- Check `_workspace.CurrentSolution.GetDocumentIdsWithFilePath()`

### Editor Features Not Working

- Check MEF exports in controller classes
- Verify content type is "tst"
- Check Output Window for exceptions

---

## Performance Considerations

### Lazy Loading

All code model collections use lazy loading:

```csharp
private IPropertyCollection _properties;
public override IPropertyCollection Properties =>
    _properties ?? (_properties = PropertyImpl.FromMetadata(...));
```

### Caching

- Templates are cached in `TemplateController`
- Metadata is NOT cached between renders (fresh analysis each time)

### Event Queue

- Rendering runs on background thread via `EventQueue`
- 1000ms delay after file save for Roslyn workspace refresh

---

## Extension Points

### Custom Template Extensions

Users can add C# code in templates:

```tst
${
    public string ToCamelCase(string s) =>
        char.ToLower(s[0]) + s.Substring(1);
}

$Properties[$ToCamelCase($Name): $Type;]
```

### External Assembly References

```tst
#reference "MyHelpers.dll"
${
    using MyHelpers;
    // Use types from MyHelpers
}
```

---

## Related Documentation

- [Project Overview](./project-overview.md)
- [Architecture](./architecture.md)
- [Source Tree Analysis](./source-tree-analysis.md)
