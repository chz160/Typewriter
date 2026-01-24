using System.Text.Json;
using System.Text.Json.Serialization;
using Typewriter.CLI.Generation;
using Typewriter.CLI.Infrastructure;

namespace Typewriter.CLI.Output;

/// <summary>
/// JSON output model for CI/CD pipeline integration.
/// </summary>
public class JsonOutputResult
{
    /// <summary>
    /// Whether the generation was successful (no errors).
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// The CLI version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Whether this was a dry run.
    /// </summary>
    [JsonPropertyName("dryRun")]
    public bool DryRun { get; set; }

    /// <summary>
    /// The solution or project path that was processed.
    /// </summary>
    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Number of templates found.
    /// </summary>
    [JsonPropertyName("templateCount")]
    public int TemplateCount { get; set; }

    /// <summary>
    /// Number of files generated.
    /// </summary>
    [JsonPropertyName("filesGenerated")]
    public int FilesGenerated { get; set; }

    /// <summary>
    /// Number of errors encountered.
    /// </summary>
    [JsonPropertyName("errorCount")]
    public int ErrorCount { get; set; }

    /// <summary>
    /// Number of warnings encountered.
    /// </summary>
    [JsonPropertyName("warningCount")]
    public int WarningCount { get; set; }

    /// <summary>
    /// Duration of the generation process.
    /// </summary>
    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;

    /// <summary>
    /// Duration in milliseconds for programmatic use.
    /// </summary>
    [JsonPropertyName("durationMs")]
    public long DurationMs { get; set; }

    /// <summary>
    /// List of generated files.
    /// </summary>
    [JsonPropertyName("files")]
    public List<JsonFileInfo> Files { get; set; } = new();

    /// <summary>
    /// List of errors encountered during generation.
    /// </summary>
    [JsonPropertyName("errors")]
    public List<JsonDiagnostic> Errors { get; set; } = new();

    /// <summary>
    /// List of warnings encountered during generation.
    /// </summary>
    [JsonPropertyName("warnings")]
    public List<JsonDiagnostic> Warnings { get; set; } = new();

    /// <summary>
    /// Creates a JSON output result from generation results.
    /// </summary>
    public static JsonOutputResult Create(
        string version,
        string targetPath,
        int templateCount,
        IReadOnlyList<GeneratedFile> generatedFiles,
        TimeSpan duration,
        bool dryRun,
        CliErrorReporter? errorReporter)
    {
        var result = new JsonOutputResult
        {
            Version = version,
            Target = targetPath,
            TemplateCount = templateCount,
            FilesGenerated = generatedFiles.Count,
            DryRun = dryRun,
            Duration = FormatDuration(duration),
            DurationMs = (long)duration.TotalMilliseconds,
            ErrorCount = errorReporter?.ErrorCount ?? 0,
            WarningCount = errorReporter?.WarningCount ?? 0,
            Success = (errorReporter?.ErrorCount ?? 0) == 0
        };

        // Add generated files
        foreach (var file in generatedFiles)
        {
            result.Files.Add(new JsonFileInfo
            {
                Path = file.RelativePath,
                Template = Path.GetFileName(file.TemplatePath),
                Bytes = file.ByteCount
            });
        }

        // Add errors
        if (errorReporter != null)
        {
            foreach (var error in errorReporter.Diagnostics.Errors)
            {
                result.Errors.Add(new JsonDiagnostic
                {
                    Message = error.Message,
                    File = error.FilePath,
                    Line = error.Line,
                    Column = error.Column
                });
            }

            // Add warnings
            foreach (var warning in errorReporter.Diagnostics.Warnings)
            {
                result.Warnings.Add(new JsonDiagnostic
                {
                    Message = warning.Message,
                    File = warning.FilePath,
                    Line = warning.Line,
                    Column = warning.Column
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Serializes the result to JSON.
    /// </summary>
    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        return JsonSerializer.Serialize(this, options);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds >= 1)
        {
            return $"{duration.TotalSeconds:F1}s";
        }
        return $"{duration.TotalMilliseconds:F0}ms";
    }
}

/// <summary>
/// JSON model for a generated file.
/// </summary>
public class JsonFileInfo
{
    /// <summary>
    /// Relative path of the generated file.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Name of the template that generated this file.
    /// </summary>
    [JsonPropertyName("template")]
    public string Template { get; set; } = string.Empty;

    /// <summary>
    /// Size of the generated file in bytes.
    /// </summary>
    [JsonPropertyName("bytes")]
    public int Bytes { get; set; }
}

/// <summary>
/// JSON model for a diagnostic message (error or warning).
/// </summary>
public class JsonDiagnostic
{
    /// <summary>
    /// The diagnostic message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The file where the diagnostic occurred (optional).
    /// </summary>
    [JsonPropertyName("file")]
    public string? File { get; set; }

    /// <summary>
    /// The line number where the diagnostic occurred (optional).
    /// </summary>
    [JsonPropertyName("line")]
    public int? Line { get; set; }

    /// <summary>
    /// The column number where the diagnostic occurred (optional).
    /// </summary>
    [JsonPropertyName("column")]
    public int? Column { get; set; }
}
