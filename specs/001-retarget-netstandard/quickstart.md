# Quickstart: Retarget CodeModel and Metadata to .NET Standard 2.0

**Feature**: 001-retarget-netstandard
**Date**: 2026-01-11

## Overview

This quickstart guides you through verifying the .NET Standard 2.0 retargeting of
Typewriter.CodeModel and Typewriter.Metadata libraries.

## Prerequisites

- Visual Studio 2022 (v17.x) or later
- .NET SDK 8.0 or later (for verification testing)
- .NET Framework 4.7.2 Developer Pack

## Verification Steps

### Step 1: Build the Retargeted Projects

```bash
# Navigate to repository root
cd E:\GitHub\Typewriter

# Build CodeModel individually
dotnet build src/CodeModel/Typewriter.CodeModel.csproj -c Release

# Build Metadata individually
dotnet build src/Metadata/Typewriter.Metadata.csproj -c Release
```

**Expected Output**: Both projects build successfully with no errors.

### Step 2: Build the Full Solution

```bash
# Using MSBuild (required for VS extension projects)
msbuild Typewriter.sln /p:Configuration=Release

# Or using the full path to VS MSBuild
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe" Typewriter.sln /p:Configuration=Release
```

**Expected Output**: Solution builds with 0 errors. The VS extension and Roslyn
projects successfully reference the netstandard2.0 assemblies.

### Step 3: Run Existing Tests

```bash
# Run all tests
dotnet test src/Tests/Typewriter.Tests.csproj

# Or using vstest.console
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\Extensions\TestPlatform\vstest.console.exe" src/Tests/bin/Release/net472/Typewriter.Tests.dll
```

**Expected Output**: All existing tests pass. No regressions from the retargeting.

### Step 4: Verify Assembly Targeting

```bash
# Check that CodeModel targets netstandard2.0
dotnet list src/CodeModel/Typewriter.CodeModel.csproj package

# Or inspect the assembly with ildasm/ILSpy
# The assembly should show TargetFramework: .NETStandard,Version=v2.0
```

### Step 5: Cross-Platform Verification (Optional)

Create a test .NET 8 console application to verify the libraries work:

```bash
# Create test project
mkdir test-netstandard
cd test-netstandard
dotnet new console -n TestConsumer -f net8.0

# Add project references
dotnet add TestConsumer reference ../src/CodeModel/Typewriter.CodeModel.csproj
dotnet add TestConsumer reference ../src/Metadata/Typewriter.Metadata.csproj

# Build to verify
dotnet build TestConsumer
```

**Expected Output**: The .NET 8 project successfully references and builds with
the netstandard2.0 libraries.

## Troubleshooting

### Build Error: CS0234 (type or namespace not found)

**Cause**: Missing System.* reference in netstandard2.0
**Solution**: Add explicit package reference:
```xml
<PackageReference Include="System.Xml.ReaderWriter" Version="4.3.1" />
```

### Build Error: Duplicate AssemblyInfo attributes

**Cause**: Both auto-generated and manual AssemblyInfo.cs exist
**Solution**: Ensure `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` is set

### Build Error: Project reference not found

**Cause**: Legacy projects expecting GUID-based reference
**Solution**: Verify ProjectReference paths are correct; GUIDs can be removed from
SDK-style projects

### Runtime Error: Assembly version mismatch

**Cause**: Binding redirect needed in net472 consumer
**Solution**: Add binding redirect in app.config of consuming project

## Rollback

If issues cannot be resolved, revert to the original project files:

```bash
git checkout -- src/CodeModel/Typewriter.CodeModel.csproj
git checkout -- src/Metadata/Typewriter.Metadata.csproj
git clean -xfd src/CodeModel/bin src/CodeModel/obj
git clean -xfd src/Metadata/bin src/Metadata/obj
```

## Success Criteria Checklist

- [ ] `dotnet build src/CodeModel/Typewriter.CodeModel.csproj` succeeds
- [ ] `dotnet build src/Metadata/Typewriter.Metadata.csproj` succeeds
- [ ] `msbuild Typewriter.sln /p:Configuration=Release` succeeds with 0 errors
- [ ] All unit tests pass
- [ ] No new compiler warnings related to framework targeting
- [ ] (Optional) .NET 8 test consumer builds successfully
