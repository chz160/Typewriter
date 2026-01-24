# CLI Interface Contract: Typewriter CLI

**Feature Branch**: `001-typewriter-cli`
**Date**: 2026-01-11

## Command Structure

### Primary Command

```
typewriter <command> [options]
```

### Commands

| Command | Description | MVP |
|---------|-------------|-----|
| generate | Generate TypeScript files from C# sources | Yes |
| version | Display version information | Yes |
| help | Display help information | Yes |

## Generate Command

### Synopsis

```
typewriter generate [--solution <path>] [--project <path>] [--config <path>]
                    [--verbose] [--quiet] [--dry-run] [--json]
                    [--help] [--version]
```

### Options

#### MVP Options

| Option | Alias | Type | Required | Default | Description |
|--------|-------|------|----------|---------|-------------|
| --solution | -s | path | No* | null | Path to .sln file |
| --project | -p | path | No* | null | Path to .csproj file |
| --config | -c | path | No | null | Path to config file |
| --help | -h | flag | No | false | Show help |
| --version | -v | flag | No | false | Show version |

*Either --solution OR --project OR config file required

#### Growth Phase Options

| Option | Alias | Type | Required | Default | Description |
|--------|-------|------|----------|---------|-------------|
| --verbose | (none) | flag | No | false | Detailed file-by-file output |
| --quiet | -q | flag | No | false | Suppress non-error output |
| --dry-run | -n | flag | No | false | Preview without writing files |
| --json | (none) | flag | No | false | JSON-formatted output |

### Mutual Exclusivity

- `--solution` and `--project` are mutually exclusive
- `--verbose` and `--quiet` are mutually exclusive

### Exit Codes

| Code | Name | Description |
|------|------|-------------|
| 0 | Success | All files generated successfully |
| 1 | GenerationFailure | Template errors, missing files, compilation errors |
| 2 | InvalidArguments | Bad CLI args, missing required options, config errors |

### Stream Usage

| Stream | Content |
|--------|---------|
| stdout | Normal output, success messages, file lists |
| stderr | Error messages, warnings |

## Output Formats

### Plain Text (Default)

**Success Output**:
```
Typewriter CLI v1.0.0
Processing solution: ./MyProject.sln
Found 3 templates

Generated 12 TypeScript files:
  - Models/Customer.ts
  - Models/Order.ts
  - Models/Product.ts
  ...

Completed in 1.2s
```

**Verbose Output** (--verbose):
```
Typewriter CLI v1.0.0
Processing solution: ./MyProject.sln
Found 3 templates

[1/3] Processing CustomerModel.tst
  → Generated: Models/Customer.ts (245 bytes)
  → Generated: Models/CustomerDto.ts (189 bytes)
[2/3] Processing OrderModel.tst
  → Generated: Models/Order.ts (312 bytes)
[3/3] Processing ProductModel.tst
  → Generated: Models/Product.ts (178 bytes)

Generated 12 TypeScript files in 1.2s
```

**Quiet Output** (--quiet):
```
(no output on success, only errors to stderr)
```

**Error Output**:
```
Error: Template compilation failed
  CustomerModel.tst:15:8: Cannot resolve type 'OrderStatus'
  OrderModel.tst:23:4: Missing closing brace

2 errors, 0 warnings
```

**Warning Output**:
```
Typewriter CLI v1.0.0
Processing solution: ./MyProject.sln
Found 3 templates

Warning: CustomerModel.tst:10:0: Referenced file 'DeletedModel.cs' not found
Warning: OrderModel.tst:5:0: Type 'LegacyEnum' is deprecated

Generated 10 TypeScript files (2 warnings) in 1.1s
```

### JSON Output (--json)

**Success**:
```json
{
  "success": true,
  "version": "1.0.0",
  "solution": "./MyProject.sln",
  "templates": 3,
  "filesGenerated": 12,
  "duration": "1.2s",
  "files": [
    {"path": "Models/Customer.ts", "bytes": 245, "template": "CustomerModel.tst"},
    {"path": "Models/Order.ts", "bytes": 312, "template": "OrderModel.tst"}
  ],
  "warnings": [],
  "errors": []
}
```

**Error**:
```json
{
  "success": false,
  "version": "1.0.0",
  "solution": "./MyProject.sln",
  "templates": 3,
  "filesGenerated": 0,
  "duration": "0.8s",
  "files": [],
  "warnings": [],
  "errors": [
    {
      "file": "CustomerModel.tst",
      "line": 15,
      "column": 8,
      "message": "Cannot resolve type 'OrderStatus'"
    }
  ]
}
```

**Dry Run**:
```json
{
  "success": true,
  "version": "1.0.0",
  "dryRun": true,
  "solution": "./MyProject.sln",
  "templates": 3,
  "filesWouldGenerate": 12,
  "duration": "0.9s",
  "files": [
    {"path": "Models/Customer.ts", "bytes": 245, "template": "CustomerModel.tst", "exists": false},
    {"path": "Models/Order.ts", "bytes": 312, "template": "OrderModel.tst", "exists": true}
  ],
  "warnings": [],
  "errors": []
}
```

## Error Message Format

### Compiler-Style Errors

Pattern: `{FilePath}:{Line}:{Column}: {Severity}: {Message}`

Examples:
```
CustomerModel.tst:15:8: error: Cannot resolve type 'OrderStatus'
OrderModel.tst:23:4: error: Missing closing brace
ProductModel.tst:10:0: warning: Type 'LegacyEnum' is deprecated
```

### Argument Errors

```
Error: Required option missing. Specify --solution or --project, or provide a config file.

Usage: typewriter generate [options]

Options:
  -s, --solution <path>  Path to .sln file
  -p, --project <path>   Path to .csproj file
  -c, --config <path>    Path to config file
  -h, --help             Show help information
  -v, --version          Show version information

For more information, run: typewriter generate --help
```

### File Not Found

```
Error: Solution file not found: ./NonExistent.sln
```

### Invalid Config

```
Error: Invalid configuration file: .typewriterrc
  Line 5: Unknown property 'soluton' (did you mean 'solution'?)
```

## Help Output

### Main Help (typewriter --help)

```
Typewriter CLI - Generate TypeScript from C# using templates

Usage: typewriter <command> [options]

Commands:
  generate    Generate TypeScript files from C# sources

Options:
  -h, --help       Show help information
  -v, --version    Show version information

Run 'typewriter <command> --help' for more information on a command.
```

### Generate Help (typewriter generate --help)

```
Generate TypeScript files from C# sources using .tst templates

Usage: typewriter generate [options]

Options:
  -s, --solution <path>  Path to solution file (.sln)
  -p, --project <path>   Path to project file (.csproj)
  -c, --config <path>    Path to configuration file
      --verbose          Show detailed file-by-file output
  -q, --quiet            Suppress non-error output
  -n, --dry-run          Preview without writing files
      --json             Output in JSON format
  -h, --help             Show help information

Examples:
  typewriter generate --solution ./MyProject.sln
  typewriter generate --project ./Api/Api.csproj
  typewriter generate --config ./typewriter.json
  typewriter generate (uses .typewriterrc if present)
```

## Version Output

```
Typewriter CLI v1.0.0
.NET 8.0
Roslyn 4.14.0
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| TYPEWRITER_CONFIG | Default config file path | null |
| NO_COLOR | Disable ANSI color output | false |
| TERM | Terminal type (affects color support) | auto-detect |

## Configuration File Priority

1. `--config` command line argument
2. `TYPEWRITER_CONFIG` environment variable
3. `.typewriterrc` in current directory
4. `typewriter.json` in current directory
5. `.typewriterrc` in solution root
6. `typewriter.json` in solution root

## Argument Priority

Command line arguments override config file values:

```
# Config file says solution: "./Default.sln"
# But CLI says --solution ./Override.sln
# Result: ./Override.sln is used
```
