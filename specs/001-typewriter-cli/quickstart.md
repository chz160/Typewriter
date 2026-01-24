# Quickstart Guide: Typewriter CLI

**Feature Branch**: `001-typewriter-cli`
**Date**: 2026-01-11

## Prerequisites

- Windows 10/11
- .NET 8 runtime installed
- Existing Typewriter .tst templates in your project

## Installation

### From Source (Development)

```bash
# Clone and build
git clone https://github.com/chz160/Typewriter.git
cd Typewriter
dotnet build src/CLI/Typewriter.CLI.csproj -c Release

# Add to PATH (PowerShell)
$env:PATH += ";$(pwd)\src\CLI\bin\Release\net8.0"
```

### From NuGet (Future)

```bash
dotnet tool install --global Typewriter.CLI
```

## Basic Usage

### Generate TypeScript from a Solution

```bash
typewriter generate --solution ./MyProject.sln
```

### Generate TypeScript from a Project

```bash
typewriter generate --project ./Api/Api.csproj
```

### Check Version

```bash
typewriter --version
```

## Example Workflow

1. **Make changes to C# models**

   Edit your `Customer.cs`:
   ```csharp
   public class Customer
   {
       public int Id { get; set; }
       public string Name { get; set; }
       public string Email { get; set; }  // New property
   }
   ```

2. **Run Typewriter CLI**

   ```bash
   typewriter generate --solution ./MyProject.sln
   ```

   Output:
   ```
   Typewriter CLI v1.0.0
   Processing solution: ./MyProject.sln
   Found 3 templates

   Generated 12 TypeScript files in 1.2s
   ```

3. **Check generated TypeScript**

   `Customer.ts` now includes:
   ```typescript
   export interface Customer {
       id: number;
       name: string;
       email: string;  // New property
   }
   ```

## Configuration File

Create `.typewriterrc` in your project root for team-standard configuration:

```json
{
  "solution": "./MyProject.sln",
  "templates": ["**/*.tst"],
  "exclude": ["**/obj/**", "**/bin/**"],
  "verbosity": "normal"
}
```

Then run without arguments:

```bash
typewriter generate
```

## Common Options

| Option | Description |
|--------|-------------|
| `--solution <path>` | Path to solution file |
| `--project <path>` | Path to project file |
| `--config <path>` | Path to config file |
| `--verbose` | Show detailed output |
| `--quiet` | Suppress non-error output |
| `--dry-run` | Preview without writing |
| `--json` | JSON output format |
| `--help` | Show help |
| `--version` | Show version |

## Troubleshooting

### "Solution file not found"

Ensure the path is correct and the file exists:

```bash
# Check the file exists
ls ./MyProject.sln

# Use absolute path if needed
typewriter generate --solution "C:\Projects\MyProject\MyProject.sln"
```

### "No templates found"

Check that .tst files exist in your solution:

```bash
# Find template files
dir *.tst /s
```

### Template Compilation Errors

The CLI shows error location:

```
Error: Template compilation failed
  CustomerModel.tst:15:8: Cannot resolve type 'OrderStatus'
```

Open the template at the specified line and fix the issue.

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Generation failed |
| 2 | Invalid arguments |

Use in scripts:

```bash
typewriter generate --solution ./MyProject.sln
if ($LASTEXITCODE -ne 0) {
    Write-Error "Typewriter generation failed"
    exit $LASTEXITCODE
}
```

## CI/CD Integration

### GitHub Actions

```yaml
- name: Generate TypeScript
  run: |
    dotnet tool install --global Typewriter.CLI
    typewriter generate --solution ./MyProject.sln
```

### Azure DevOps

```yaml
- script: |
    dotnet tool install --global Typewriter.CLI
    typewriter generate --solution ./MyProject.sln
  displayName: 'Generate TypeScript'
```

## Next Steps

- [Full CLI Reference](./contracts/cli-interface.md)
- [Configuration Schema](./contracts/config-schema.json)
- [Data Model](./data-model.md)

## Getting Help

```bash
# General help
typewriter --help

# Command-specific help
typewriter generate --help
```

Report issues: https://github.com/chz160/Typewriter/issues
