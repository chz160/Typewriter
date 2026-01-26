using Typewriter.CLI.Infrastructure.Models;

namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// Discovers source files for a project using glob patterns.
/// </summary>
public interface ISourceFileDiscovery
{
    /// <summary>
    /// Discovers source files for an SDK-style project.
    /// </summary>
    /// <param name="projectDirectory">Project root directory.</param>
    /// <param name="includePatterns">Additional include patterns (beyond defaults).</param>
    /// <param name="excludePatterns">Additional exclude patterns (beyond defaults).</param>
    /// <param name="removePatterns">Explicit remove patterns.</param>
    /// <returns>Source file discovery result.</returns>
    SourceFileResult DiscoverSdkStyleFiles(
        string projectDirectory,
        IEnumerable<string>? includePatterns = null,
        IEnumerable<string>? excludePatterns = null,
        IEnumerable<string>? removePatterns = null);

    /// <summary>
    /// Discovers source files for a legacy project with explicit includes.
    /// </summary>
    /// <param name="projectDirectory">Project root directory.</param>
    /// <param name="explicitIncludes">Explicit file include patterns.</param>
    /// <returns>Source file discovery result.</returns>
    SourceFileResult DiscoverLegacyFiles(
        string projectDirectory,
        IEnumerable<string> explicitIncludes);
}
