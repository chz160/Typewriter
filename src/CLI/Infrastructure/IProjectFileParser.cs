using Typewriter.CLI.Infrastructure.Models;

namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// Parses .csproj files to extract metadata without MSBuild evaluation.
/// </summary>
public interface IProjectFileParser
{
    /// <summary>
    /// Parses a .csproj file and returns project information.
    /// </summary>
    /// <param name="projectPath">Absolute path to .csproj file.</param>
    /// <returns>Parsed project information, or null if parsing fails.</returns>
    ProjectInfo? Parse(string projectPath);

    /// <summary>
    /// Determines if a project file is SDK-style.
    /// </summary>
    /// <param name="projectPath">Absolute path to .csproj file.</param>
    /// <returns>True if SDK-style, false if legacy.</returns>
    bool IsSdkStyleProject(string projectPath);
}
