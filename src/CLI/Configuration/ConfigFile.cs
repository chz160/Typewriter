using System.Text.Json;
using System.Text.Json.Serialization;

namespace Typewriter.CLI.Configuration;

/// <summary>
/// Represents a Typewriter configuration file (.typewriterrc or typewriter.json).
/// </summary>
public class ConfigFile
{
    /// <summary>
    /// Path to the solution file (.sln).
    /// </summary>
    [JsonPropertyName("solution")]
    public string? Solution { get; set; }

    /// <summary>
    /// Path to the project file (.csproj). Mutually exclusive with Solution.
    /// </summary>
    [JsonPropertyName("project")]
    public string? Project { get; set; }

    /// <summary>
    /// Template include patterns (glob patterns). Defaults to ["**/*.tst"].
    /// </summary>
    [JsonPropertyName("templates")]
    public List<string>? Templates { get; set; }

    /// <summary>
    /// Template exclude patterns (glob patterns). Defaults to ["**/obj/**", "**/bin/**", "**/node_modules/**"].
    /// </summary>
    [JsonPropertyName("exclude")]
    public List<string>? Exclude { get; set; }

    /// <summary>
    /// Output directory override. If not specified, outputs next to templates.
    /// </summary>
    [JsonPropertyName("output")]
    public string? Output { get; set; }

    /// <summary>
    /// Verbosity level: "quiet", "normal", or "verbose".
    /// </summary>
    [JsonPropertyName("verbosity")]
    public string? Verbosity { get; set; }

    /// <summary>
    /// The path to the config file (set when loaded).
    /// </summary>
    [JsonIgnore]
    public string? ConfigPath { get; set; }

    /// <summary>
    /// Gets the directory containing the config file.
    /// </summary>
    [JsonIgnore]
    public string? ConfigDirectory => ConfigPath != null ? Path.GetDirectoryName(ConfigPath) : null;

    /// <summary>
    /// Default template patterns.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultTemplatePatterns = new[] { "**/*.tst" };

    /// <summary>
    /// Default exclude patterns.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultExcludePatterns = new[]
    {
        "**/obj/**",
        "**/bin/**",
        "**/node_modules/**"
    };

    /// <summary>
    /// Creates a new config file with default values.
    /// </summary>
    public static ConfigFile CreateDefault()
    {
        return new ConfigFile
        {
            Templates = new List<string>(DefaultTemplatePatterns),
            Exclude = new List<string>(DefaultExcludePatterns),
            Verbosity = "normal"
        };
    }

    /// <summary>
    /// Resolves a path relative to the config file location.
    /// </summary>
    /// <param name="relativePath">The relative path to resolve.</param>
    /// <returns>The absolute path.</returns>
    public string ResolvePath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return relativePath;
        }

        var baseDir = ConfigDirectory ?? Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(baseDir, relativePath));
    }

    /// <summary>
    /// Gets the resolved solution path.
    /// </summary>
    public string? GetResolvedSolutionPath()
    {
        return Solution != null ? ResolvePath(Solution) : null;
    }

    /// <summary>
    /// Gets the resolved project path.
    /// </summary>
    public string? GetResolvedProjectPath()
    {
        return Project != null ? ResolvePath(Project) : null;
    }

    /// <summary>
    /// Gets the resolved output directory.
    /// </summary>
    public string? GetResolvedOutputPath()
    {
        return Output != null ? ResolvePath(Output) : null;
    }

    /// <summary>
    /// Parses the verbosity setting into quiet/verbose flags.
    /// </summary>
    /// <returns>A tuple of (quiet, verbose) flags.</returns>
    public (bool Quiet, bool Verbose) ParseVerbosity()
    {
        return Verbosity?.ToLowerInvariant() switch
        {
            "quiet" => (true, false),
            "verbose" => (false, true),
            _ => (false, false)
        };
    }
}

/// <summary>
/// Handles loading and discovering configuration files.
/// </summary>
public static class ConfigFileLoader
{
    /// <summary>
    /// Standard config file names in order of preference.
    /// </summary>
    public static readonly string[] ConfigFileNames = new[]
    {
        ".typewriterrc",
        "typewriter.json",
        ".typewriterrc.json"
    };

    /// <summary>
    /// Loads a config file from a specific path.
    /// </summary>
    /// <param name="configPath">The path to the config file.</param>
    /// <returns>The loaded config file, or null if it doesn't exist or is invalid.</returns>
    public static ConfigFile? LoadFromPath(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var config = JsonSerializer.Deserialize<ConfigFile>(json, options);
            if (config != null)
            {
                config.ConfigPath = Path.GetFullPath(configPath);
            }
            return config;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Discovers and loads a config file from standard locations.
    /// Search order: current directory, then solution/project root.
    /// </summary>
    /// <param name="startDirectory">The directory to start searching from.</param>
    /// <param name="solutionOrProjectPath">Optional solution or project path to search near.</param>
    /// <returns>The loaded config file, or null if none found.</returns>
    public static ConfigFile? Discover(string? startDirectory = null, string? solutionOrProjectPath = null)
    {
        var searchDirs = new List<string>();

        // 1. Current directory (highest priority)
        var currentDir = startDirectory ?? Directory.GetCurrentDirectory();
        searchDirs.Add(currentDir);

        // 2. Solution/project directory
        if (!string.IsNullOrEmpty(solutionOrProjectPath))
        {
            var slnDir = Path.GetDirectoryName(Path.GetFullPath(solutionOrProjectPath));
            if (slnDir != null && !searchDirs.Contains(slnDir, StringComparer.OrdinalIgnoreCase))
            {
                searchDirs.Add(slnDir);
            }
        }

        // Search each directory for config files
        foreach (var dir in searchDirs)
        {
            foreach (var fileName in ConfigFileNames)
            {
                var configPath = Path.Combine(dir, fileName);
                var config = LoadFromPath(configPath);
                if (config != null)
                {
                    return config;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Merges CLI arguments with config file settings.
    /// CLI arguments take precedence over config file settings.
    /// </summary>
    /// <param name="config">The config file (can be null).</param>
    /// <param name="cliSolution">CLI --solution argument.</param>
    /// <param name="cliProject">CLI --project argument.</param>
    /// <param name="cliVerbose">CLI --verbose flag.</param>
    /// <param name="cliQuiet">CLI --quiet flag.</param>
    /// <returns>Merged settings.</returns>
    public static MergedSettings Merge(
        ConfigFile? config,
        string? cliSolution,
        string? cliProject,
        bool cliVerbose,
        bool cliQuiet)
    {
        var settings = new MergedSettings();

        // Solution/Project: CLI takes precedence
        if (!string.IsNullOrEmpty(cliSolution))
        {
            settings.SolutionPath = cliSolution;
        }
        else if (!string.IsNullOrEmpty(cliProject))
        {
            settings.ProjectPath = cliProject;
        }
        else if (config != null)
        {
            settings.SolutionPath = config.GetResolvedSolutionPath();
            settings.ProjectPath = config.GetResolvedProjectPath();
        }

        // Verbosity: CLI takes precedence
        if (cliVerbose)
        {
            settings.Verbose = true;
            settings.Quiet = false;
        }
        else if (cliQuiet)
        {
            settings.Quiet = true;
            settings.Verbose = false;
        }
        else if (config != null)
        {
            var (quiet, verbose) = config.ParseVerbosity();
            settings.Quiet = quiet;
            settings.Verbose = verbose;
        }

        // Template patterns: use config or defaults
        settings.TemplatePatterns = config?.Templates ?? new List<string>(ConfigFile.DefaultTemplatePatterns);
        settings.ExcludePatterns = config?.Exclude ?? new List<string>(ConfigFile.DefaultExcludePatterns);

        // Output directory: from config only (no CLI override in current spec)
        settings.OutputDirectory = config?.GetResolvedOutputPath();

        // Store config path for reference
        settings.ConfigPath = config?.ConfigPath;

        return settings;
    }
}

/// <summary>
/// Represents merged settings from config file and CLI arguments.
/// </summary>
public class MergedSettings
{
    /// <summary>
    /// Path to the solution file.
    /// </summary>
    public string? SolutionPath { get; set; }

    /// <summary>
    /// Path to the project file.
    /// </summary>
    public string? ProjectPath { get; set; }

    /// <summary>
    /// Whether to use verbose output.
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Whether to use quiet output.
    /// </summary>
    public bool Quiet { get; set; }

    /// <summary>
    /// Template include patterns.
    /// </summary>
    public List<string> TemplatePatterns { get; set; } = new();

    /// <summary>
    /// Template exclude patterns.
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new();

    /// <summary>
    /// Output directory override.
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Path to the config file that was loaded (if any).
    /// </summary>
    public string? ConfigPath { get; set; }

    /// <summary>
    /// Whether a config file was loaded.
    /// </summary>
    public bool HasConfig => ConfigPath != null;

    /// <summary>
    /// Gets the target path (solution or project).
    /// </summary>
    public string? TargetPath => SolutionPath ?? ProjectPath;

    /// <summary>
    /// Gets whether a solution is the target (vs project).
    /// </summary>
    public bool IsSolution => SolutionPath != null;
}
