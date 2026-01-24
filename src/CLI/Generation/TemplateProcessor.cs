using System.Text.RegularExpressions;
using Typewriter.CLI.Configuration;
using Typewriter.CLI.Infrastructure;
using Typewriter.CodeModel.Implementation;
using Typewriter.Core.Abstractions;
using File = Typewriter.CodeModel.File;

namespace Typewriter.CLI.Generation;

/// <summary>
/// Processes Typewriter templates and generates TypeScript output.
/// Handles error reporting with file:line:column information.
/// </summary>
public class TemplateProcessor
{
    private readonly CliMetadataProvider _metadataProvider;
    private readonly CliRoslynWorkspace _workspace;
    private readonly IErrorReporter _errorReporter;
    private readonly bool _verbose;

    public TemplateProcessor(
        CliMetadataProvider metadataProvider,
        CliRoslynWorkspace workspace,
        IErrorReporter errorReporter,
        bool verbose = false)
    {
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _errorReporter = errorReporter ?? throw new ArgumentNullException(nameof(errorReporter));
        _verbose = verbose;
    }

    /// <summary>
    /// Processes a template file and generates TypeScript output.
    /// </summary>
    /// <param name="templatePath">Path to the .tst template file.</param>
    /// <param name="dryRun">If true, no files will be written.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the template processing.</returns>
    public async Task<TemplateResult> ProcessAsync(
        string templatePath,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var result = new TemplateResult(templatePath);

        try
        {
            // Validate template file exists
            if (!System.IO.File.Exists(templatePath))
            {
                _errorReporter.ReportError($"Template file not found: {templatePath}", templatePath);
                return result;
            }

            // Read template content for syntax validation
            string templateContent;
            try
            {
                templateContent = await System.IO.File.ReadAllTextAsync(templatePath, cancellationToken);
            }
            catch (Exception ex)
            {
                _errorReporter.ReportError($"Failed to read template: {ex.Message}", templatePath);
                return result;
            }

            // Validate template syntax
            var syntaxErrors = ValidateTemplateSyntax(templatePath, templateContent);
            foreach (var error in syntaxErrors)
            {
                if (error.IsError)
                {
                    _errorReporter.ReportError(error.Message, templatePath, error.Line, error.Column);
                }
                else
                {
                    _errorReporter.ReportWarning(error.Message, templatePath, error.Line, error.Column);
                }
            }

            if (syntaxErrors.Any(e => e.IsError))
            {
                return result;
            }

            // Create CLI template with the new template engine
            var solutionPath = _workspace.SolutionPath ?? _workspace.ProjectPath ?? Path.GetDirectoryName(templatePath)!;
            CliTemplate template;
            try
            {
                template = new CliTemplate(templatePath, solutionPath, _errorReporter, _metadataProvider);
            }
            catch (Exception ex)
            {
                _errorReporter.ReportError($"Failed to compile template: {ex.Message}", templatePath);
                return result;
            }

            // Check if template compilation failed
            if (template.HasCompileException)
            {
                return result;
            }

            // Get files to render based on template settings
            var matchingFiles = template.GetFilesToRender();

            if (_verbose)
            {
                _errorReporter.ReportInfo($"  Found {matchingFiles.Count} matching C# file(s)");
            }

            // Check for single-file mode
            if (template.Settings.IsSingleFileMode)
            {
                await ProcessSingleFileModeAsync(template, matchingFiles, dryRun, result, cancellationToken);
            }
            else
            {
                await ProcessMultiFileModeAsync(template, matchingFiles, dryRun, result, cancellationToken);
            }

            result.Success = !_errorReporter.HasErrors;
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError($"Unexpected error: {ex.Message}", templatePath);
        }

        return result;
    }

    private async Task ProcessMultiFileModeAsync(
        CliTemplate template,
        ICollection<string> matchingFiles,
        bool dryRun,
        TemplateResult result,
        CancellationToken cancellationToken)
    {
        foreach (var sourceFile in matchingFiles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var outputPath = await RenderFileAsync(template, sourceFile, dryRun, cancellationToken);

                if (outputPath != null)
                {
                    var relativePath = Path.GetRelativePath(
                        Path.GetDirectoryName(template.TemplatePath)!,
                        outputPath);

                    var byteCount = dryRun ? 0 : (int)new FileInfo(outputPath).Length;
                    result.AddGeneratedFile(new GeneratedFile(relativePath, template.TemplatePath, byteCount));
                }
            }
            catch (TemplateRenderException ex)
            {
                _errorReporter.ReportError(ex.Message, ex.FilePath ?? template.TemplatePath, ex.Line, ex.Column);
            }
            catch (Exception ex)
            {
                _errorReporter.ReportError(
                    $"Error processing {Path.GetFileName(sourceFile)}: {ex.Message}",
                    sourceFile);
            }
        }
    }

    private async Task ProcessSingleFileModeAsync(
        CliTemplate template,
        ICollection<string> matchingFiles,
        bool dryRun,
        TemplateResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            // Collect all file metadata
            var files = new List<File>();
            var settings = new CliSettings(
                _workspace.SolutionPath ?? _workspace.ProjectPath ?? string.Empty,
                template.TemplatePath);

            foreach (var sourceFile in matchingFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var fileMetadata = _metadataProvider.GetFile(sourceFile, settings, _ => { });
                if (fileMetadata != null)
                {
                    files.Add(new FileImpl(fileMetadata, settings));
                }
            }

            if (files.Count == 0)
            {
                if (_verbose)
                {
                    _errorReporter.ReportInfo("  No files matched for single-file mode");
                }
                return;
            }

            // Render all files into single output
            var outputPath = template.RenderFile(files.ToArray(), dryRun);

            if (outputPath != null)
            {
                var relativePath = Path.GetRelativePath(
                    Path.GetDirectoryName(template.TemplatePath)!,
                    outputPath);

                var byteCount = dryRun ? 0 : (int)new FileInfo(outputPath).Length;
                result.AddGeneratedFile(new GeneratedFile(relativePath, template.TemplatePath, byteCount));
            }
        }
        catch (TemplateRenderException ex)
        {
            _errorReporter.ReportError(ex.Message, ex.FilePath ?? template.TemplatePath, ex.Line, ex.Column);
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError($"Error in single-file mode: {ex.Message}", template.TemplatePath);
        }
    }

    private async Task<string?> RenderFileAsync(
        CliTemplate template,
        string sourceFile,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        // Get file metadata
        var settings = new CliSettings(
            _workspace.SolutionPath ?? _workspace.ProjectPath ?? string.Empty,
            template.TemplatePath);

        var fileMetadata = _metadataProvider.GetFile(sourceFile, settings, _ => { });
        if (fileMetadata == null)
        {
            if (_verbose)
            {
                _errorReporter.ReportWarning(
                    $"Could not load metadata for {Path.GetFileName(sourceFile)}",
                    sourceFile);
            }
            return null;
        }

        // Create File implementation for the template engine
        var file = new FileImpl(fileMetadata, settings);

        // Render the file using the template
        var outputPath = template.RenderFile(file, dryRun);

        return outputPath;
    }

    private List<TemplateSyntaxError> ValidateTemplateSyntax(string templatePath, string content)
    {
        var errors = new List<TemplateSyntaxError>();
        var lines = content.Split('\n');

        for (int lineNum = 0; lineNum < lines.Length; lineNum++)
        {
            var line = lines[lineNum];
            var lineNumber = lineNum + 1; // 1-based

            // Check for unclosed code blocks
            var openBraces = 0;
            var inCodeBlock = false;
            for (int col = 0; col < line.Length; col++)
            {
                if (col > 0 && line[col - 1] == '$' && line[col] == '{')
                {
                    inCodeBlock = true;
                    openBraces++;
                }
                else if (inCodeBlock && line[col] == '{')
                {
                    openBraces++;
                }
                else if (inCodeBlock && line[col] == '}')
                {
                    openBraces--;
                    if (openBraces == 0)
                    {
                        inCodeBlock = false;
                    }
                }
            }

            // Check for unmatched brackets in filters/blocks
            var openBrackets = line.Count(c => c == '[') - line.Count(c => c == ']');
            var openParens = line.Count(c => c == '(') - line.Count(c => c == ')');

            // These are warnings, not errors, as they might span multiple lines
            if (openBrackets > 0 && !line.TrimEnd().EndsWith("["))
            {
                // Only warn if there's a clear mismatch on a single line
            }

            // Check for invalid identifier patterns
            var invalidPattern = Regex.Match(line, @"\$\s+\w");
            if (invalidPattern.Success)
            {
                errors.Add(new TemplateSyntaxError(
                    "Space between $ and identifier is not allowed",
                    lineNumber,
                    invalidPattern.Index + 1,
                    IsError: false)); // Warning
            }

            // Check for potentially missing template block after collection identifier
            var collectionPattern = Regex.Match(line, @"\$(?:Classes|Properties|Methods|Parameters|Interfaces|Enums)\s*$");
            if (collectionPattern.Success)
            {
                errors.Add(new TemplateSyntaxError(
                    "Collection identifier without template block - did you forget []?",
                    lineNumber,
                    collectionPattern.Index + 1,
                    IsError: false)); // Warning
            }
        }

        // Check for unclosed code blocks across the file
        var totalOpen = content.Split(new[] { "${" }, StringSplitOptions.None).Length - 1;
        var codeBlockMatches = Regex.Matches(content, @"\$\{[^}]*\}");
        if (codeBlockMatches.Count < totalOpen)
        {
            errors.Add(new TemplateSyntaxError(
                "Unclosed code block (${...}) detected",
                1,
                null,
                IsError: true));
        }

        return errors;
    }

    private TemplateSettings ParseTemplateSettings(string templatePath, string content)
    {
        var settings = new TemplateSettings
        {
            TemplatePath = templatePath,
            OutputDirectory = Path.GetDirectoryName(templatePath)!,
            OutputExtension = ".ts"
        };

        // Look for filter pattern in first $Classes(), $Interfaces(), etc.
        var filterMatch = Regex.Match(content, @"\$(?:Classes|Interfaces|Enums|Records|Delegates)\(([^)]*)\)");
        if (filterMatch.Success)
        {
            settings.FilterPattern = filterMatch.Groups[1].Value.Trim();
        }

        // Look for Settings code block
        var settingsMatch = Regex.Match(content, @"\$\{[^}]*Settings\s*\([^)]*\)[^}]*\}", RegexOptions.Singleline);
        if (settingsMatch.Success)
        {
            var settingsBlock = settingsMatch.Value;

            // Parse OutputExtension
            var extMatch = Regex.Match(settingsBlock, @"OutputExtension\s*=\s*""([^""]+)""");
            if (extMatch.Success)
            {
                settings.OutputExtension = extMatch.Groups[1].Value;
            }

            // Parse OutputDirectory
            var dirMatch = Regex.Match(settingsBlock, @"OutputDirectory\s*=\s*""([^""]+)""");
            if (dirMatch.Success)
            {
                var outputDir = dirMatch.Groups[1].Value;
                if (!Path.IsPathRooted(outputDir))
                {
                    outputDir = Path.Combine(Path.GetDirectoryName(templatePath)!, outputDir);
                }
                settings.OutputDirectory = outputDir;
            }

            // Parse IncludeProject
            var projMatch = Regex.Match(settingsBlock, @"IncludeProject\s*\(\s*""([^""]+)""\s*\)");
            if (projMatch.Success)
            {
                settings.IncludeProjectPattern = projMatch.Groups[1].Value;
            }
        }

        return settings;
    }

    private List<string> FindMatchingFiles(string? filterPattern)
    {
        var files = new List<string>();

        // Get all C# files from the metadata provider's workspace
        var allFiles = _metadataProvider.GetAllSourceFiles();

        foreach (var file in allFiles)
        {
            if (string.IsNullOrEmpty(filterPattern))
            {
                files.Add(file);
            }
            else
            {
                // Apply filter pattern (e.g., "*Model" matches files ending with Model)
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (MatchesFilter(fileName, filterPattern))
                {
                    files.Add(file);
                }
            }
        }

        return files;
    }

    private static bool MatchesFilter(string name, string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern == "*")
        {
            return true;
        }

        // Convert simple wildcard pattern to regex
        var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(name, regex, RegexOptions.IgnoreCase);
    }

    private GeneratedFile? GenerateOutput(
        string templatePath,
        string templateContent,
        string sourceFile,
        TemplateSettings settings,
        bool dryRun)
    {
        // Get file metadata
        var fileMetadata = _metadataProvider.GetFileMetadata(sourceFile);
        if (fileMetadata == null)
        {
            _errorReporter.ReportWarning(
                $"Could not load metadata for {Path.GetFileName(sourceFile)}",
                sourceFile);
            return null;
        }

        // Determine output path
        var sourceFileName = Path.GetFileNameWithoutExtension(sourceFile);
        var outputFileName = sourceFileName + settings.OutputExtension;
        var outputPath = Path.Combine(settings.OutputDirectory, outputFileName);

        // For now, return a placeholder result
        // Full template rendering will be implemented when we integrate the template engine
        var relativePath = Path.GetRelativePath(
            Path.GetDirectoryName(templatePath)!,
            outputPath);

        if (!dryRun)
        {
            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // TODO: Write actual generated content
            // For now, this is a placeholder that demonstrates the error handling flow
        }

        return new GeneratedFile(relativePath, templatePath, 0);
    }
}

/// <summary>
/// Represents a syntax error found during template validation.
/// </summary>
public record TemplateSyntaxError(
    string Message,
    int Line,
    int? Column,
    bool IsError);

/// <summary>
/// Settings parsed from a template file.
/// </summary>
public class TemplateSettings
{
    public string TemplatePath { get; set; } = string.Empty;
    public string? FilterPattern { get; set; }
    public string OutputDirectory { get; set; } = string.Empty;
    public string OutputExtension { get; set; } = ".ts";
    public string? IncludeProjectPattern { get; set; }
}

/// <summary>
/// Result of processing a single template.
/// </summary>
public class TemplateResult
{
    private readonly List<GeneratedFile> _generatedFiles = new();

    public TemplateResult(string templatePath)
    {
        TemplatePath = templatePath;
    }

    public string TemplatePath { get; }
    public bool Success { get; set; }
    public IReadOnlyList<GeneratedFile> GeneratedFiles => _generatedFiles;

    public void AddGeneratedFile(GeneratedFile file)
    {
        _generatedFiles.Add(file);
    }
}

/// <summary>
/// Information about a generated file.
/// </summary>
public record GeneratedFile(
    string RelativePath,
    string TemplatePath,
    int ByteCount);

/// <summary>
/// Exception thrown when template rendering fails.
/// </summary>
public class TemplateRenderException : Exception
{
    public string? FilePath { get; }
    public int? Line { get; }
    public int? Column { get; }

    public TemplateRenderException(string message, string? filePath = null, int? line = null, int? column = null)
        : base(message)
    {
        FilePath = filePath;
        Line = line;
        Column = column;
    }
}
