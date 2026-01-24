namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// Provides path resolution utilities for the CLI.
/// </summary>
public class PathResolver
{
    private readonly string _rootPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="PathResolver"/> class.
    /// </summary>
    /// <param name="rootPath">The root path for resolving relative paths.</param>
    public PathResolver(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
    }

    /// <summary>
    /// Gets the root path that paths are resolved relative to.
    /// </summary>
    public string RootPath => _rootPath;

    /// <summary>
    /// Resolves a relative path to an absolute path based on the root path.
    /// </summary>
    /// <param name="relativePath">The relative path to resolve.</param>
    /// <returns>The absolute path.</returns>
    public string ResolveAbsolutePath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return NormalizePath(relativePath);
        }

        return NormalizePath(Path.Combine(_rootPath, relativePath));
    }

    /// <summary>
    /// Resolves a relative path based on a template file's location.
    /// </summary>
    /// <param name="templatePath">The path to the template file.</param>
    /// <param name="relativePath">The relative path to resolve.</param>
    /// <returns>The resolved absolute path.</returns>
    public string ResolveRelativeToTemplate(string templatePath, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return NormalizePath(relativePath);
        }

        var templateDir = Path.GetDirectoryName(templatePath) ?? _rootPath;
        return NormalizePath(Path.Combine(templateDir, relativePath));
    }

    /// <summary>
    /// Gets the relative path from the root to the specified absolute path.
    /// </summary>
    /// <param name="absolutePath">The absolute path.</param>
    /// <returns>The relative path from root.</returns>
    public string GetRelativePathFromRoot(string absolutePath)
    {
        var normalizedAbsolute = NormalizePath(absolutePath);
        var normalizedRoot = _rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (normalizedAbsolute.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = normalizedAbsolute.Substring(normalizedRoot.Length);
            return relativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        return Path.GetRelativePath(_rootPath, absolutePath);
    }

    /// <summary>
    /// Normalizes a path by resolving '..' and '.' segments and using consistent separators.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        return Path.GetFullPath(path);
    }

    /// <summary>
    /// Gets the directory containing the solution or project file.
    /// </summary>
    /// <param name="solutionOrProjectPath">Path to the .sln or .csproj file.</param>
    /// <returns>The directory path.</returns>
    public static string GetSolutionDirectory(string solutionOrProjectPath)
    {
        var fullPath = Path.GetFullPath(solutionOrProjectPath);
        return Path.GetDirectoryName(fullPath) ?? throw new ArgumentException($"Invalid path: {solutionOrProjectPath}");
    }
}
