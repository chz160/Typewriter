using Typewriter.CLI.Infrastructure.Models;

namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// Parses .sln files to extract project entries.
/// </summary>
public interface ISolutionFileParser
{
    /// <summary>
    /// Parses a .sln file and returns solution information.
    /// </summary>
    /// <param name="solutionPath">Absolute path to .sln file.</param>
    /// <returns>Parsed solution information, or null if parsing fails.</returns>
    SolutionInfo? Parse(string solutionPath);

    /// <summary>
    /// Extracts C# project paths from a solution file.
    /// </summary>
    /// <param name="solutionPath">Absolute path to .sln file.</param>
    /// <returns>Enumerable of absolute paths to .csproj files.</returns>
    IEnumerable<string> GetCSharpProjectPaths(string solutionPath);
}
