using System.CommandLine;
using System.Text.Json;
using Typewriter.CLI.Configuration;
using Typewriter.CLI.Infrastructure;
using Typewriter.CLI.Output;

namespace Typewriter.CLI.Commands;

/// <summary>
/// The init command for creating a configuration file.
/// </summary>
public static class InitCommand
{
    /// <summary>
    /// Creates the init command with all its options.
    /// </summary>
    public static Command Create()
    {
        var forceOption = new Option<bool>(
            aliases: ["--force", "-f"],
            description: "Overwrite existing config file");

        var formatOption = new Option<string>(
            aliases: ["--format"],
            description: "Config file format (json or rc)",
            getDefaultValue: () => "rc");

        var command = new Command("init", "Initialize a Typewriter configuration file")
        {
            forceOption,
            formatOption
        };

        command.SetHandler((force, format) =>
        {
            var exitCode = Execute(force, format);
            Environment.ExitCode = exitCode;
        }, forceOption, formatOption);

        return command;
    }

    private static int Execute(bool force, string format)
    {
        var output = new ConsoleOutput();

        // Determine config file name based on format
        var fileName = format.ToLowerInvariant() switch
        {
            "json" => "typewriter.json",
            _ => ".typewriterrc"
        };

        var configPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

        // Check if config already exists
        if (File.Exists(configPath) && !force)
        {
            output.Error($"Config file already exists: {fileName}");
            output.Info("Use --force to overwrite.");
            return ExitCodes.InvalidArguments;
        }

        // Check for existing config files
        foreach (var existingName in ConfigFileLoader.ConfigFileNames)
        {
            var existingPath = Path.Combine(Directory.GetCurrentDirectory(), existingName);
            if (File.Exists(existingPath) && existingPath != configPath && !force)
            {
                output.Warning($"Found existing config file: {existingName}");
                output.Info($"Creating {fileName} anyway. Use --force to overwrite.");
            }
        }

        try
        {
            // Try to auto-detect solution/project
            string? solution = null;
            string? project = null;

            var currentDir = Directory.GetCurrentDirectory();
            var slnFiles = Directory.GetFiles(currentDir, "*.sln");
            if (slnFiles.Length == 1)
            {
                solution = Path.GetFileName(slnFiles[0]);
            }
            else
            {
                var projFiles = Directory.GetFiles(currentDir, "*.csproj");
                if (projFiles.Length == 1)
                {
                    project = Path.GetFileName(projFiles[0]);
                }
            }

            // Create config content
            var config = new ConfigFile
            {
                Solution = solution,
                Project = solution == null ? project : null,
                Templates = new List<string>(ConfigFile.DefaultTemplatePatterns),
                Exclude = new List<string>(ConfigFile.DefaultExcludePatterns),
                Verbosity = "normal"
            };

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(config, jsonOptions);
            File.WriteAllText(configPath, json);

            output.Success($"Created {fileName}");

            if (solution != null)
            {
                output.Info($"  Auto-detected solution: {solution}");
            }
            else if (project != null)
            {
                output.Info($"  Auto-detected project: {project}");
            }
            else
            {
                output.Warning("  No solution or project detected. Edit the config file to specify one.");
            }

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            output.Error($"Failed to create config file: {ex.Message}");
            return ExitCodes.GenerationFailure;
        }
    }
}
