using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// Discovers Typewriter template (.tst) files within a solution or project.
/// </summary>
public class TemplateFinder
{
    private readonly string _rootPath;
    private readonly string[] _includePatterns;
    private readonly string[] _excludePatterns;

    /// <summary>
    /// Default patterns for finding template files.
    /// </summary>
    public static readonly string[] DefaultIncludePatterns = new[] { "**/*.tst" };

    /// <summary>
    /// Default patterns for excluding directories.
    /// </summary>
    public static readonly string[] DefaultExcludePatterns = new[] { "**/obj/**", "**/bin/**", "**/node_modules/**" };

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateFinder"/> class.
    /// </summary>
    /// <param name="rootPath">The root path to search from.</param>
    /// <param name="includePatterns">Glob patterns to include (defaults to **/*.tst).</param>
    /// <param name="excludePatterns">Glob patterns to exclude (defaults to obj/bin/node_modules).</param>
    public TemplateFinder(
        string rootPath,
        string[]? includePatterns = null,
        string[]? excludePatterns = null)
    {
        _rootPath = Path.GetFullPath(rootPath);
        _includePatterns = includePatterns ?? DefaultIncludePatterns;
        _excludePatterns = excludePatterns ?? DefaultExcludePatterns;
    }

    /// <summary>
    /// Finds all template files matching the patterns.
    /// </summary>
    /// <returns>A collection of absolute paths to template files.</returns>
    public IReadOnlyList<string> FindTemplates()
    {
        var matcher = new Matcher();

        foreach (var pattern in _includePatterns)
        {
            matcher.AddInclude(pattern);
        }

        foreach (var pattern in _excludePatterns)
        {
            matcher.AddExclude(pattern);
        }

        var directoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(_rootPath));
        var result = matcher.Execute(directoryInfo);

        return result.Files
            .Select(f => Path.GetFullPath(Path.Combine(_rootPath, f.Path)))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Finds all template files in the specified directory.
    /// </summary>
    /// <param name="directory">The directory to search.</param>
    /// <returns>A collection of absolute paths to template files.</returns>
    public static IReadOnlyList<string> FindInDirectory(string directory)
    {
        var finder = new TemplateFinder(directory);
        return finder.FindTemplates();
    }

    /// <summary>
    /// Finds all template files relative to a solution or project file.
    /// </summary>
    /// <param name="solutionOrProjectPath">The path to the .sln or .csproj file.</param>
    /// <returns>A collection of absolute paths to template files.</returns>
    public static IReadOnlyList<string> FindForSolutionOrProject(string solutionOrProjectPath)
    {
        var fullPath = Path.GetFullPath(solutionOrProjectPath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException($"Invalid path: {solutionOrProjectPath}");

        return FindInDirectory(directory);
    }

    /// <summary>
    /// Gets the template name (file name without extension) from a path.
    /// </summary>
    /// <param name="templatePath">The full path to the template.</param>
    /// <returns>The template name.</returns>
    public static string GetTemplateName(string templatePath)
    {
        return Path.GetFileNameWithoutExtension(templatePath);
    }

    /// <summary>
    /// Gets the relative path of a template from the root.
    /// </summary>
    /// <param name="templatePath">The full path to the template.</param>
    /// <param name="rootPath">The root path to calculate relative from.</param>
    /// <returns>The relative path.</returns>
    public static string GetRelativePath(string templatePath, string rootPath)
    {
        return Path.GetRelativePath(rootPath, templatePath);
    }
}
