namespace Typewriter.Core.Abstractions
{
    /// <summary>
    /// Provides methods for resolving file paths during template generation.
    /// </summary>
    public interface IPathResolver
    {
        /// <summary>
        /// Resolves a relative path to an absolute path based on the root path.
        /// </summary>
        /// <param name="relativePath">The relative path to resolve.</param>
        /// <returns>The absolute path.</returns>
        string ResolveAbsolutePath(string relativePath);

        /// <summary>
        /// Resolves a relative path based on a template file's location.
        /// </summary>
        /// <param name="templatePath">The path to the template file.</param>
        /// <param name="relativePath">The relative path to resolve.</param>
        /// <returns>The resolved absolute path.</returns>
        string ResolveRelativeToTemplate(string templatePath, string relativePath);

        /// <summary>
        /// Gets the relative path from the root to the specified absolute path.
        /// </summary>
        /// <param name="absolutePath">The absolute path.</param>
        /// <returns>The relative path from root.</returns>
        string GetRelativePathFromRoot(string absolutePath);

        /// <summary>
        /// Normalizes a path by resolving '..' and '.' segments and using consistent separators.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        string NormalizePath(string path);

        /// <summary>
        /// Gets the root path that paths are resolved relative to.
        /// </summary>
        string RootPath { get; }
    }
}
