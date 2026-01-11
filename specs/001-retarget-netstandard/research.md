# Research: Retarget CodeModel and Metadata to .NET Standard 2.0

**Feature**: 001-retarget-netstandard
**Date**: 2026-01-11
**Status**: Complete

## Research Summary

This document consolidates technical research for migrating Typewriter.CodeModel and
Typewriter.Metadata from .NET Framework 4.7.2 to .NET Standard 2.0.

---

## Decision 1: Target Framework Selection

**Decision**: Target `netstandard2.0`

**Rationale**:
- .NET Standard 2.0 is the broadest compatibility standard
- Supported by .NET Framework 4.6.1+ (our VS extension targets 4.7.2)
- Supported by .NET Core 2.0+ and .NET 5/6/7/8
- Provides 32,000+ APIs - sufficient for CodeModel and Metadata projects
- No external dependencies in either project that would require newer standards

**Alternatives Considered**:

| Alternative | Rejected Because |
|-------------|------------------|
| netstandard2.1 | Not compatible with .NET Framework 4.7.2 (VS extension requirement) |
| Multi-targeting (netstandard2.0;net472) | Unnecessary complexity; netstandard2.0 alone provides needed compatibility |
| net8.0 | Would break .NET Framework 4.7.2 compatibility |

---

## Decision 2: Project File Format

**Decision**: Convert to SDK-style .csproj format

**Rationale**:
- SDK-style format is required for netstandard targeting
- Dramatically simpler project files (removes explicit file listings)
- Enables `dotnet build` and `dotnet restore` CLI commands
- Better tooling support in modern IDEs
- Automatic globbing includes all .cs files by default

**Alternatives Considered**:

| Alternative | Rejected Because |
|-------------|------------------|
| Keep legacy format | Legacy format does not support netstandard targeting |
| Hybrid approach | No practical benefit; adds maintenance burden |

---

## Decision 3: AssemblyInfo Handling

**Decision**: Preserve existing AssemblyInfo.cs files with `GenerateAssemblyInfo=false`

**Rationale**:
- Maintains consistency with upstream project
- Preserves existing version numbers and assembly metadata
- Avoids potential conflicts from auto-generated attributes
- SharedAssemblyInfo.cs link pattern continues to work

**Alternatives Considered**:

| Alternative | Rejected Because |
|-------------|------------------|
| Auto-generate AssemblyInfo | Would lose custom metadata; risk of duplicate attributes |
| Delete AssemblyInfo files | Would break SharedAssemblyInfo linking pattern |

---

## Decision 4: Root Namespace Configuration

**Decision**: Explicitly set RootNamespace in project files

**CodeModel**: `<RootNamespace>Typewriter</RootNamespace>`
**Metadata**: `<RootNamespace>Typewriter.Metadata</RootNamespace>`

**Rationale**:
- CodeModel historically uses `Typewriter` as root (not `Typewriter.CodeModel`)
- This matches the existing namespace structure in source files
- Metadata uses `Typewriter.Metadata` as expected
- Explicit configuration prevents SDK-style auto-inference from folder name

**Alternatives Considered**:

| Alternative | Rejected Because |
|-------------|------------------|
| Let SDK infer namespaces | Would incorrectly set CodeModel to `Typewriter.CodeModel` |
| Rename source namespaces | Breaking change; violates upstream compatibility |

---

## Decision 5: Implicit Usings

**Decision**: Do not enable implicit usings (`ImplicitUsings` absent/false)

**Rationale**:
- Existing code has explicit using statements
- Avoids any risk of compilation errors from missing imports
- Maintains consistency with upstream codebase style
- Explicit usings preferred per constitution (Principle I)

**Alternatives Considered**:

| Alternative | Rejected Because |
|-------------|------------------|
| Enable implicit usings | Could cause CS0246 errors if any using was missed |

---

## Decision 6: Nullable Reference Types

**Decision**: Do not enable nullable reference types

**Rationale**:
- Existing code not annotated for nullability
- Enabling would generate numerous warnings
- Not required for netstandard2.0 functionality
- Can be addressed in a future enhancement if desired

**Alternatives Considered**:

| Alternative | Rejected Because |
|-------------|------------------|
| Enable nullable | Out of scope; would require significant code annotations |

---

## API Compatibility Analysis

### Typewriter.CodeModel APIs Used

| Namespace | Available in netstandard2.0 | Notes |
|-----------|----------------------------|-------|
| System | Yes | Core types |
| System.Collections.Generic | Yes | IEnumerable, List, Dictionary |
| System.Linq | Yes | LINQ extensions |
| System.Text | Yes | StringBuilder |
| System.Xml | Yes | Referenced but minimal usage |

**Result**: All APIs used are available in netstandard2.0

### Typewriter.Metadata APIs Used

| Namespace | Available in netstandard2.0 | Notes |
|-----------|----------------------------|-------|
| System | Yes | Core types |
| System.Collections.Generic | Yes | IEnumerable |
| Typewriter.CodeModel | Yes (self) | Internal project reference |

**Result**: All APIs used are available in netstandard2.0

---

## Potential Issues and Mitigations

### Issue 1: System.Xml Reference in CodeModel

**Analysis**: The legacy project includes `<Reference Include="System.Xml" />`.
SDK-style projects targeting netstandard2.0 include System.Xml automatically.

**Mitigation**: No action needed. If issues arise, add:
```xml
<PackageReference Include="System.Xml.ReaderWriter" Version="4.3.1" />
```

### Issue 2: SharedAssemblyInfo.cs Link

**Analysis**: Both projects link to `src/Typewriter/Properties/SharedAssemblyInfo.cs`.
SDK-style projects use `<Compile Include="..." Link="..." />` syntax.

**Mitigation**: The link can be preserved with:
```xml
<ItemGroup>
  <Compile Include="..\Typewriter\Properties\SharedAssemblyInfo.cs" Link="Properties\SharedAssemblyInfo.cs" />
</ItemGroup>
```

### Issue 3: Legacy Project References

**Analysis**: Dependent projects (Roslyn, Typewriter, Tests) use legacy .csproj format
and reference CodeModel/Metadata via `<ProjectReference>` with explicit GUID.

**Mitigation**: Legacy projects can reference SDK-style projects. The GUID in
`<ProjectReference>` is optional and can be removed, or the existing reference
format should continue to work.

### Issue 4: Documentation File Generation

**Analysis**: Legacy projects specify `<DocumentationFile>` with configuration-specific
paths. SDK-style projects use `<GenerateDocumentationFile>true</GenerateDocumentationFile>`.

**Mitigation**: Use SDK-style documentation generation:
```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

---

## Final SDK-Style Project Files

### Typewriter.CodeModel.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <RootNamespace>Typewriter</RootNamespace>
    <AssemblyName>Typewriter.CodeModel</AssemblyName>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="..\Typewriter\Properties\SharedAssemblyInfo.cs" Link="Properties\SharedAssemblyInfo.cs" />
  </ItemGroup>
</Project>
```

### Typewriter.Metadata.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <RootNamespace>Typewriter.Metadata</RootNamespace>
    <AssemblyName>Typewriter.Metadata</AssemblyName>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="..\Typewriter\Properties\SharedAssemblyInfo.cs" Link="Properties\SharedAssemblyInfo.cs" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\CodeModel\Typewriter.CodeModel.csproj" />
  </ItemGroup>
</Project>
```

---

## Verification Steps

1. **Build individual projects**:
   ```bash
   dotnet build src/CodeModel/Typewriter.CodeModel.csproj
   dotnet build src/Metadata/Typewriter.Metadata.csproj
   ```

2. **Build full solution**:
   ```bash
   msbuild Typewriter.sln /p:Configuration=Release
   ```

3. **Run tests**:
   ```bash
   dotnet test src/Tests/Typewriter.Tests.csproj
   ```

4. **Verify assembly targets**:
   ```bash
   dotnet list src/CodeModel/Typewriter.CodeModel.csproj package
   ```

---

## References

- [.NET Standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
- [SDK-style projects](https://docs.microsoft.com/en-us/dotnet/core/project-sdk/overview)
- [netstandard2.0 API browser](https://docs.microsoft.com/en-us/dotnet/api/?view=netstandard-2.0)
- [Migration guide](https://docs.microsoft.com/en-us/dotnet/core/porting/)
