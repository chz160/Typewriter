using System.Reflection;
using System.Text;
using Typewriter.CLI.Configuration;
using Typewriter.CLI.Infrastructure;
using Typewriter.CodeModel;
using Typewriter.Configuration;
using Typewriter.Core.Abstractions;
using File = Typewriter.CodeModel.File;
using Path = System.IO.Path;
using Type = System.Type;

namespace Typewriter.CLI.Generation;

/// <summary>
/// CLI-specific template class that processes .tst files without Visual Studio dependencies.
/// This is a port of the VS extension's Template class with VS-specific code removed.
/// </summary>
public class CliTemplate
{
    private readonly List<Type> _customExtensions = new();
    private readonly string _templatePath;
    private readonly string _solutionPath;
    private readonly IErrorReporter _errorReporter;
    private readonly CliMetadataProvider _metadataProvider;
    private Lazy<string> _template;
    private Lazy<CliSettings> _configuration;
    private bool _templateCompileException;
    private bool _templateCompiled;
    private object? _templateInstance;

    /// <summary>
    /// Gets the template settings.
    /// </summary>
    public Settings Settings => _configuration.Value;

    /// <summary>
    /// Gets the path to the template file.
    /// </summary>
    public string TemplatePath => _templatePath;

    /// <summary>
    /// Gets whether the template has been compiled.
    /// </summary>
    public bool IsCompiled => _templateCompiled;

    /// <summary>
    /// Gets whether a compile exception occurred.
    /// </summary>
    public bool HasCompileException => _templateCompileException;

    /// <summary>
    /// Gets the compiled template instance for invoking custom methods.
    /// </summary>
    public object? TemplateInstance
    {
        get
        {
            // Ensure configuration is initialized, which creates the instance
            _ = _configuration.Value;
            return _templateInstance;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliTemplate"/> class.
    /// </summary>
    /// <param name="templatePath">The path to the .tst template file.</param>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <param name="errorReporter">The error reporter for logging.</param>
    /// <param name="metadataProvider">The metadata provider for accessing C# files.</param>
    public CliTemplate(
        string templatePath,
        string solutionPath,
        IErrorReporter errorReporter,
        CliMetadataProvider metadataProvider)
    {
        _templatePath = templatePath ?? throw new ArgumentNullException(nameof(templatePath));
        _solutionPath = solutionPath ?? throw new ArgumentNullException(nameof(solutionPath));
        _errorReporter = errorReporter ?? throw new ArgumentNullException(nameof(errorReporter));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));

        _template = CreateLazyTemplate();
        _configuration = CreateLazyConfiguration();
    }

    private Lazy<CliSettings> CreateLazyConfiguration()
    {
        return new Lazy<CliSettings>(() =>
        {
            var settings = new CliSettings(_solutionPath, Path.GetFullPath(_templatePath));

            // Force template initialization to load custom extensions
            if (!_template.IsValueCreated)
            {
                _ = _template.Value;
            }

            // If template has a custom class with Settings constructor, invoke it
            // Note: The constructor may be non-public (no access modifier defaults to private in C#)
            var templateClass = _customExtensions.FirstOrDefault();
            if (templateClass == null)
            {
                // No custom template code - this is OK, just means no OutputFilenameFactory
            }
            else
            {
                // Look for constructor with Settings parameter, including non-public constructors
                var ctor = templateClass.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(Settings) },
                    null);

                if (ctor != null)
                {
                    try
                    {
                        // Create and store the template instance for later use in method invocations
                        _templateInstance = ctor.Invoke(new object[] { settings });
                    }
                    catch (Exception ex)
                    {
                        var innerMessage = ex.InnerException?.Message ?? ex.Message;
                        _errorReporter.ReportWarning(
                            $"Failed to invoke template settings constructor: {innerMessage}",
                            _templatePath);
                    }
                }
                else
                {
                    // Try parameterless constructor
                    var defaultCtor = templateClass.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, Type.EmptyTypes, null);

                    if (defaultCtor != null)
                    {
                        try
                        {
                            _templateInstance = defaultCtor.Invoke(Array.Empty<object>());
                        }
                        catch
                        {
                            // Ignore - instance methods won't work without instance
                        }
                    }
                }
            }

            return settings;
        });
    }

    private Lazy<string> CreateLazyTemplate()
    {
        _templateCompiled = false;
        _templateCompileException = false;

        return new Lazy<string>(() =>
        {
            var code = System.IO.File.ReadAllText(_templatePath);
            try
            {
                var result = CliTemplateCodeParser.Parse(_templatePath, code, _customExtensions, _errorReporter);
                _templateCompiled = true;
                return result;
            }
            catch (Exception ex)
            {
                _templateCompileException = true;
                _errorReporter.ReportError($"Template compilation failed: {ex.Message}", _templatePath);
                throw;
            }
        });
    }

    /// <summary>
    /// Gets the list of C# files that should be rendered with this template.
    /// </summary>
    /// <returns>A collection of file paths to render.</returns>
    public ICollection<string> GetFilesToRender()
    {
        var allFiles = _metadataProvider.GetAllSourceFiles();

        // If no specific projects are included, return all files
        var includedProjects = _configuration.Value.IncludedProjects;
        if (includedProjects.Count == 0)
        {
            return allFiles.ToList();
        }

        // Filter files based on included projects
        return allFiles
            .Where(f => ShouldRenderFile(f))
            .ToList();
    }

    /// <summary>
    /// Determines whether a file should be rendered based on project inclusion settings.
    /// </summary>
    /// <param name="filename">The file path to check.</param>
    /// <returns>True if the file should be rendered; otherwise, false.</returns>
    public bool ShouldRenderFile(string filename)
    {
        var includedProjects = _configuration.Value.IncludedProjects;

        // If no projects are specifically included, render all files
        if (includedProjects.Count == 0)
        {
            return true;
        }

        // Check if the file's directory contains any of the included project names
        var directory = Path.GetDirectoryName(filename) ?? string.Empty;
        return includedProjects.Any(p =>
            directory.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Renders a single file using the template.
    /// </summary>
    /// <param name="file">The file metadata to render.</param>
    /// <param name="success">Output parameter indicating whether rendering succeeded.</param>
    /// <returns>The rendered output, or null if no match was found.</returns>
    public string? Render(File file, out bool success)
    {
        try
        {
            return CliParser.Parse(_templatePath, file.FullName, _template.Value, _customExtensions, TemplateInstance, file, _errorReporter, out success);
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError($"{ex.Message} Template: {_templatePath}", _templatePath);
            success = false;
            return null;
        }
    }

    /// <summary>
    /// Renders multiple files in single-file mode.
    /// </summary>
    /// <param name="files">The files to render.</param>
    /// <param name="success">Output parameter indicating whether rendering succeeded.</param>
    /// <returns>The rendered output, or null if rendering failed.</returns>
    public string? Render(File[] files, out bool success)
    {
        try
        {
            return CliSingleFileParser.Parse(_templatePath, files, _template.Value, _customExtensions, TemplateInstance, _errorReporter, out success);
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError($"{ex.Message} Template: {_templatePath}", _templatePath);
            success = false;
            return null;
        }
    }

    /// <summary>
    /// Renders a file and writes the output.
    /// </summary>
    /// <param name="file">The file to render.</param>
    /// <param name="dryRun">If true, don't write files.</param>
    /// <returns>The output path if successful, null otherwise.</returns>
    public string? RenderFile(File file, bool dryRun = false)
    {
        var output = Render(file, out var success);

        if (success)
        {
            if (output == null)
            {
                // No output means no match - nothing to write
                return null;
            }

            var outputPath = GetOutputPath(file);

            if (string.Equals(file.FullName, outputPath, StringComparison.OrdinalIgnoreCase))
            {
                _errorReporter.ReportError("Output filename cannot match source filename.", file.FullName);
                return null;
            }

            if (!dryRun)
            {
                if (HasChanged(outputPath, output))
                {
                    WriteFile(outputPath, output);
                }
            }

            return outputPath;
        }

        return null;
    }

    /// <summary>
    /// Renders multiple files in single-file mode.
    /// </summary>
    /// <param name="files">The files to render.</param>
    /// <param name="dryRun">If true, don't write files.</param>
    /// <returns>The output path if successful, null otherwise.</returns>
    public string? RenderFile(File[] files, bool dryRun = false)
    {
        var output = Render(files, out var success);

        if (success && output != null)
        {
            var outputDir = GetOutputDirectory();
            var singleFileName = Settings.SingleFileName;
            var outputPath = Path.Combine(outputDir, singleFileName);

            if (!dryRun)
            {
                WriteFile(outputPath, output);
            }

            return outputPath;
        }

        return null;
    }

    /// <summary>
    /// Writes content to a file.
    /// </summary>
    /// <param name="outputPath">The path to write to.</param>
    /// <param name="outputContent">The content to write.</param>
    protected virtual void WriteFile(string outputPath, string outputContent)
    {
        try
        {
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            System.IO.File.WriteAllText(
                outputPath,
                outputContent,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: Settings.Utf8BomGeneration));
        }
        catch (Exception exception)
        {
            _errorReporter.ReportError(
                $"Unable to write file '{outputPath}': {exception.Message}",
                outputPath);
        }
    }

    /// <summary>
    /// Gets the output path for a file based on template settings.
    /// </summary>
    /// <param name="file">The source file.</param>
    /// <returns>The output file path.</returns>
    public string GetOutputPath(File file)
    {
        var directory = GetOutputDirectory();
        var filename = GetOutputFilename(file, file.FullName);
        return Path.Combine(directory, filename);
    }

    /// <summary>
    /// Gets the output directory based on template settings.
    /// </summary>
    /// <returns>The output directory path.</returns>
    public string GetOutputDirectory()
    {
        var directory = _configuration.Value.OutputDirectory;
        if (string.IsNullOrEmpty(directory))
        {
            return Path.GetDirectoryName(_templatePath) ?? string.Empty;
        }

        var templateDirectory = Path.GetDirectoryName(_templatePath);

        if (!string.IsNullOrEmpty(templateDirectory) && !Path.IsPathRooted(directory))
        {
            directory = Path.Combine(templateDirectory, directory);
        }

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return directory;
    }

    private string GetOutputFilename(File file, string sourcePath)
    {
        var sourceFilename = Path.GetFileNameWithoutExtension(sourcePath);
        var extension = GetOutputExtension();

        try
        {
            if (_configuration.Value.OutputFilenameFactory != null)
            {
                var filename = _configuration.Value.OutputFilenameFactory(file);

                filename = filename
                    .Replace("<", "-")
                    .Replace(">", "-")
                    .Replace(":", "-")
                    .Replace("\"", "-")
                    .Replace("|", "-")
                    .Replace("?", "-")
                    .Replace("*", "-");

                if (!filename.Contains('.'))
                {
                    filename += extension;
                }

                return filename;
            }
        }
        catch (Exception exception)
        {
            _errorReporter.ReportWarning(
                $"Can't get output filename for '{sourcePath}' ({exception.Message})",
                sourcePath);
        }

        return sourceFilename + extension;
    }

    private string GetOutputExtension()
    {
        var extension = _configuration.Value.OutputExtension;

        if (string.IsNullOrWhiteSpace(extension))
        {
            return ".ts";
        }

        return "." + extension.Trim('.');
    }

    private static bool HasChanged(string path, string output)
    {
        if (System.IO.File.Exists(path))
        {
            var current = System.IO.File.ReadAllText(path);
            if (string.Equals(current, output, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Reloads the template, forcing recompilation.
    /// </summary>
    public void Reload()
    {
        _template = CreateLazyTemplate();
        _configuration = CreateLazyConfiguration();
    }
}
