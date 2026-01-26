using System.CommandLine;
using System.Diagnostics;
using Typewriter.CLI.Configuration;
using Typewriter.CLI.Generation;
using Typewriter.CLI.Infrastructure;
using Typewriter.CLI.Output;

namespace Typewriter.CLI.Commands;

/// <summary>
/// The generate command for generating TypeScript files from C# sources.
/// </summary>
public static class GenerateCommand
{
    /// <summary>
    /// Creates the generate command with all its options.
    /// </summary>
    public static Command Create()
    {
        var solutionOption = new Option<FileInfo?>(
            aliases: ["--solution", "-s"],
            description: "Path to solution file (.sln)")
        {
            ArgumentHelpName = "path"
        };

        var projectOption = new Option<FileInfo?>(
            aliases: ["--project", "-p"],
            description: "Path to project file (.csproj)")
        {
            ArgumentHelpName = "path"
        };

        var configOption = new Option<FileInfo?>(
            aliases: ["--config", "-c"],
            description: "Path to configuration file")
        {
            ArgumentHelpName = "path"
        };

        var verboseOption = new Option<bool>(
            name: "--verbose",
            description: "Show detailed file-by-file output");

        var quietOption = new Option<bool>(
            aliases: ["--quiet", "-q"],
            description: "Suppress non-error output");

        var dryRunOption = new Option<bool>(
            aliases: ["--dry-run", "-n"],
            description: "Preview without writing files");

        var jsonOption = new Option<bool>(
            name: "--json",
            description: "Output in JSON format");

        var command = new Command("generate", "Generate TypeScript files from C# sources using .tst templates")
        {
            solutionOption,
            projectOption,
            configOption,
            verboseOption,
            quietOption,
            dryRunOption,
            jsonOption
        };

        command.SetHandler(async (context) =>
        {
            var solution = context.ParseResult.GetValueForOption(solutionOption);
            var project = context.ParseResult.GetValueForOption(projectOption);
            var config = context.ParseResult.GetValueForOption(configOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var json = context.ParseResult.GetValueForOption(jsonOption);

            var exitCode = await ExecuteAsync(
                solution, project, config,
                verbose, quiet, dryRun, json,
                context.GetCancellationToken());

            context.ExitCode = exitCode;
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        FileInfo? solutionFile,
        FileInfo? projectFile,
        FileInfo? configFile,
        bool verbose,
        bool quiet,
        bool dryRun,
        bool json,
        CancellationToken cancellationToken)
    {
        var output = new ConsoleOutput();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate mutually exclusive options
            if (solutionFile != null && projectFile != null)
            {
                output.Error("Options --solution and --project are mutually exclusive.");
                return ExitCodes.InvalidArguments;
            }

            if (verbose && quiet)
            {
                output.Error("Options --verbose and --quiet are mutually exclusive.");
                return ExitCodes.InvalidArguments;
            }

            // Load config file
            ConfigFile? config = null;
            if (configFile != null)
            {
                if (!File.Exists(configFile.FullName))
                {
                    output.Error($"Config file not found: {configFile.FullName}");
                    return ExitCodes.InvalidArguments;
                }
                config = ConfigFileLoader.LoadFromPath(configFile.FullName);
                if (config == null)
                {
                    output.Error($"Invalid config file: {configFile.FullName}");
                    return ExitCodes.InvalidArguments;
                }
            }
            else
            {
                // Try to discover config file
                config = ConfigFileLoader.Discover(
                    Directory.GetCurrentDirectory(),
                    solutionFile?.FullName ?? projectFile?.FullName);
            }

            // Merge config with CLI arguments (CLI takes precedence)
            var settings = ConfigFileLoader.Merge(
                config,
                solutionFile?.FullName,
                projectFile?.FullName,
                verbose,
                quiet);

            // Determine the solution or project path
            string? solutionPath = settings.SolutionPath;
            string? projectPath = settings.ProjectPath;

            // If neither specified, try to find automatically
            if (solutionPath == null && projectPath == null)
            {
                var currentDir = Directory.GetCurrentDirectory();

                // Look for .sln file
                var slnFiles = Directory.GetFiles(currentDir, "*.sln");
                if (slnFiles.Length == 1)
                {
                    solutionPath = slnFiles[0];
                }
                else if (slnFiles.Length > 1)
                {
                    output.Error("Multiple solution files found. Please specify one using --solution.");
                    return ExitCodes.InvalidArguments;
                }
                else
                {
                    // Look for .csproj file
                    var projFiles = Directory.GetFiles(currentDir, "*.csproj");
                    if (projFiles.Length == 1)
                    {
                        projectPath = projFiles[0];
                    }
                    else if (projFiles.Length > 1)
                    {
                        output.Error("Multiple project files found. Please specify one using --project or --solution.");
                        return ExitCodes.InvalidArguments;
                    }
                    else
                    {
                        output.Error("No solution or project file found. Specify --solution or --project.");
                        return ExitCodes.InvalidArguments;
                    }
                }
            }

            // Validate paths exist
            if (solutionPath != null && !File.Exists(solutionPath))
            {
                output.Error($"Solution file not found: {solutionPath}");
                return ExitCodes.InvalidArguments;
            }

            if (projectPath != null && !File.Exists(projectPath))
            {
                output.Error($"Project file not found: {projectPath}");
                return ExitCodes.InvalidArguments;
            }

            var targetPath = solutionPath ?? projectPath!;
            var targetType = solutionPath != null ? "solution" : "project";

            // Apply merged verbosity settings
            var effectiveVerbose = settings.Verbose;
            var effectiveQuiet = settings.Quiet;

            if (!effectiveQuiet)
            {
                output.Info($"Typewriter CLI v{Program.Version}");
                if (settings.HasConfig)
                {
                    output.Info($"Using config: {settings.ConfigPath}");
                }
                output.Info($"Processing {targetType}: {targetPath}");
            }

            // Initialize the workspace
            using var workspace = new CliRoslynWorkspace(effectiveQuiet ? null : output);

            if (!effectiveQuiet)
            {
                output.Info("Loading workspace...");
            }

            Infrastructure.Models.LoadingResult loadingResult;
            if (solutionPath != null)
            {
                loadingResult = await workspace.LoadSolutionWithResultAsync(solutionPath, cancellationToken);
            }
            else
            {
                loadingResult = await workspace.LoadProjectWithResultAsync(projectPath!, cancellationToken);
            }

            // Output detailed timing information when verbose
            if (effectiveVerbose && !effectiveQuiet)
            {
                output.Verbose($"  Loaded {loadingResult.ProjectCount} project(s) with {loadingResult.SourceFileCount} source file(s)");
                output.Verbose($"  Loading time: {loadingResult.LoadTime.TotalSeconds:F2}s");
                if (loadingResult.UsedFallback)
                {
                    output.Verbose($"  Used fallback loading: {loadingResult.FallbackReason}");
                }
                else
                {
                    output.Verbose("  Used fast loading (direct Roslyn)");
                }
            }

            if (!loadingResult.Success)
            {
                foreach (var diag in loadingResult.Diagnostics)
                {
                    output.Error(diag.Message);
                }
                return ExitCodes.GenerationFailure;
            }

            if (workspace.Diagnostics.HasErrors)
            {
                foreach (var diag in workspace.Diagnostics.Errors)
                {
                    output.Error(diag.Message);
                }
                return ExitCodes.GenerationFailure;
            }

            // Find templates
            var rootPath = solutionPath != null
                ? Path.GetDirectoryName(solutionPath)!
                : Path.GetDirectoryName(projectPath!)!;

            var templates = TemplateFinder.FindInDirectory(rootPath);

            if (templates.Count == 0)
            {
                output.Warning("No .tst template files found.");
                stopwatch.Stop();

                if (!effectiveQuiet)
                {
                    output.Info($"Completed in {stopwatch.Elapsed.TotalSeconds:F1}s");
                }
                return ExitCodes.Success;
            }

            if (!effectiveQuiet)
            {
                output.Info($"Found {templates.Count} template(s)");
            }

            // Create error reporter and metadata provider
            var errorReporter = new CliErrorReporter(output);
            var metadataProvider = new CliMetadataProvider(workspace);
            var templateProcessor = new TemplateProcessor(metadataProvider, workspace, errorReporter, effectiveVerbose);

            // Process templates
            var generatedFiles = new List<GeneratedFile>();

            for (var i = 0; i < templates.Count; i++)
            {
                var templatePath = templates[i];

                if (cancellationToken.IsCancellationRequested)
                {
                    output.Warning("Operation cancelled.");
                    return ExitCodes.GenerationFailure;
                }

                if (!effectiveQuiet)
                {
                    output.WriteProgress(i + 1, templates.Count, Path.GetFileName(templatePath));
                }

                try
                {
                    var result = await templateProcessor.ProcessAsync(
                        templatePath,
                        dryRun,
                        cancellationToken);

                    generatedFiles.AddRange(result.GeneratedFiles);
                }
                catch (Exception ex)
                {
                    errorReporter.ReportError($"Unexpected error: {ex.Message}", templatePath);
                }
            }

            var errorCount = errorReporter.ErrorCount;
            var warningCount = errorReporter.WarningCount;

            stopwatch.Stop();

            // Output results
            if (json)
            {
                OutputJsonResult(targetPath, templates.Count, generatedFiles, stopwatch.Elapsed, dryRun, errorReporter);
            }
            else if (!effectiveQuiet)
            {
                if (dryRun)
                {
                    output.Info($"\nDry run: Would generate {generatedFiles.Count} TypeScript file(s)");
                }
                else
                {
                    output.Success($"\nGenerated {generatedFiles.Count} TypeScript file(s)");
                }

                if (effectiveVerbose)
                {
                    foreach (var file in generatedFiles)
                    {
                        output.Info($"  - {file.RelativePath}");
                    }
                }

                if (warningCount > 0)
                {
                    output.Warning($"{warningCount} warning(s)");
                }

                if (effectiveVerbose)
                {
                    var generationTime = stopwatch.Elapsed - loadingResult.LoadTime;
                    output.Info($"Completed in {stopwatch.Elapsed.TotalSeconds:F1}s (loading: {loadingResult.LoadTime.TotalSeconds:F2}s, generation: {generationTime.TotalSeconds:F2}s)");
                }
                else
                {
                    output.Info($"Completed in {stopwatch.Elapsed.TotalSeconds:F1}s");
                }
            }

            return errorCount > 0 ? ExitCodes.GenerationFailure : ExitCodes.Success;
        }
        catch (OperationCanceledException)
        {
            output.Warning("Operation cancelled.");
            return ExitCodes.GenerationFailure;
        }
        catch (Exception ex)
        {
            output.Error($"Unexpected error: {ex.Message}");
            return ExitCodes.GenerationFailure;
        }
    }

    private static void OutputJsonResult(
        string targetPath,
        int templateCount,
        List<GeneratedFile> generatedFiles,
        TimeSpan duration,
        bool dryRun,
        CliErrorReporter errorReporter)
    {
        var result = JsonOutputResult.Create(
            Program.Version,
            targetPath,
            templateCount,
            generatedFiles,
            duration,
            dryRun,
            errorReporter);

        Console.WriteLine(result.ToJson());
    }
}
