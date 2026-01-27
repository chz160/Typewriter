# Quickstart: CLI Template Engine Parity Testing

## Overview

This guide explains how to verify CLI template engine parity with the VS extension.

## Prerequisites

- .NET 8.0 SDK installed
- Visual Studio 2022 (for building the full solution)
- A test solution with .tst template files (e.g., AcciClaim)

## Building the CLI

```bash
cd E:\GitHub\Typewriter

# Build just the CLI project
dotnet build src/CLI/Typewriter.CLI.csproj --configuration Release

# Or build the entire solution with MSBuild
"c:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe" Typewriter.sln /p:Configuration=Release
```

## Running Parity Tests

### 1. Run the Unit Test Suite

```bash
cd E:\GitHub\Typewriter

# Run all CLI tests
dotnet test src/CLI.Tests/Typewriter.CLI.Tests.csproj --configuration Release

# Run specific test class
dotnet test src/CLI.Tests/Typewriter.CLI.Tests.csproj --filter "FullyQualifiedName~TemplateParityTests"
```

### 2. Test Against a Real Solution

```bash
cd E:\GitHub\Typewriter\src\CLI\bin\Release\net8.0

# Generate TypeScript from a solution
dotnet typewriter.dll generate --solution "C:\Path\To\YourSolution.sln" --verbose

# Dry run (show what would be generated without writing)
dotnet typewriter.dll generate --solution "C:\Path\To\YourSolution.sln" --dry-run
```

### 3. Compare Output with VS Extension

1. **Generate baseline** with VS extension (save output)
2. **Generate with CLI** using same templates
3. **Diff the outputs** to find discrepancies

```bash
# Example comparison
git diff --no-index expected_output/ cli_output/
```

## Adding New Parity Tests

### Test File Locations

```
src/CLI.Tests/
├── TemplateParityTests.cs      # Template rendering tests
├── CliSettingsTests.cs         # Settings configuration tests
├── TypeResolutionTests.cs      # Type metadata tests (to add)
├── FilterSyntaxTests.cs        # Filter behavior tests (to add)
└── Fixtures/                   # Test data files
```

### Test Pattern

```csharp
[Fact]
public void Feature_Scenario_ExpectedBehavior()
{
    // Arrange
    var template = "$Classes[...]";
    var source = "public class TestClass { }";

    // Act
    var result = RenderTemplate(template, source);

    // Assert
    result.ShouldEqual("expected output");
}
```

## Key Test Scenarios

### 1. Type Resolution Tests

Test various C# types and their TypeScript mappings:

```csharp
// Test source
public class TypeTestClass
{
    public int[] ArrayProp { get; set; }           // number[]
    public List<string> ListProp { get; set; }     // string[]
    public Dictionary<string, int> DictProp { get; set; }  // { [key: string]: number }
    public int? NullableProp { get; set; }         // number | null (with StrictNullGeneration)
    public (string A, int B) TupleProp { get; set; }  // tuple type
    public Task<string> TaskProp { get; set; }     // string (unwrapped)
}
```

### 2. Filter Tests

Test all filter syntaxes:

```csharp
// Name patterns
$Classes(*Model)      // ends with "Model"
$Classes(Base*)       // starts with "Base"
$Classes(I*Service)   // contains pattern

// Attribute filters
$Classes([Serializable])
$Classes([CustomAttribute])

// Inheritance filters
$Classes(:BaseClass)
$Classes(:IInterface)

// Predicate filters
$Classes($IncludeClass)  // custom method
```

### 3. Settings Tests

Test each setting modification:

```csharp
// In template code block
${
    Template(Settings settings)
    {
        settings.DisableStrictNullGeneration();
        settings.UseStringLiteralCharacter('\'');
        settings.OutputFilenameFactory = file => file.Classes.First().Name + ".generated.ts";
    }
}
```

### 4. Boolean Conditional Tests

```csharp
// Template
$Classes[$IsAbstract[abstract ][concrete ]class $Name]

// With nested conditionals
$Properties[$HasGetter[get; ][$HasSetter[set; ][]]]
```

## Debugging Parity Issues

### Enable Verbose Logging

```bash
dotnet typewriter.dll generate --solution "..." --verbose
```

### Check Template Compilation

If methods aren't being invoked:
1. Check the compiled template class in memory
2. Verify BindingFlags include NonPublic | Instance
3. Check method parameter types match context type

### Check Settings Propagation

If settings aren't being respected:
1. Verify `template.Settings` is used (not new CliSettings)
2. Check settings object is passed through to FileImpl
3. Verify Helpers.cs receives settings for type conversion

## Success Criteria

- [ ] All 357+ existing tests pass
- [ ] AcciClaim templates generate matching output
- [ ] Type resolution tests cover all 6 type categories
- [ ] Filter tests cover all 4 filter types
- [ ] Settings tests cover all 10 settings properties
- [ ] Boolean conditional tests verify true/false/nested blocks
