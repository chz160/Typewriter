namespace Typewriter.CLI.Infrastructure.Models;

/// <summary>
/// Represents a parsed .sln file with its projects.
/// </summary>
public sealed class SolutionInfo
{
    /// <summary>
    /// Gets the absolute path to the .sln file.
    /// </summary>
    public string SolutionPath { get; }

    /// <summary>
    /// Gets the directory containing the .sln file.
    /// </summary>
    public string SolutionDirectory { get; }

    /// <summary>
    /// Gets the solution name derived from the file name.
    /// </summary>
    public string SolutionName { get; }

    /// <summary>
    /// Gets the projects in the solution.
    /// </summary>
    public IReadOnlyList<SolutionProject> Projects { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionInfo"/> class.
    /// </summary>
    /// <param name="solutionPath">Absolute path to the .sln file.</param>
    /// <param name="projects">Projects in the solution.</param>
    public SolutionInfo(string solutionPath, IReadOnlyList<SolutionProject> projects)
    {
        SolutionPath = Path.GetFullPath(solutionPath);
        SolutionDirectory = Path.GetDirectoryName(SolutionPath) ?? string.Empty;
        SolutionName = Path.GetFileNameWithoutExtension(SolutionPath);
        Projects = projects;
    }

    /// <summary>
    /// Gets only the C# projects from the solution.
    /// </summary>
    /// <returns>Enumerable of C# projects.</returns>
    public IEnumerable<SolutionProject> GetCSharpProjects()
    {
        return Projects.Where(p => p.IsCSharpProject);
    }

    /// <summary>
    /// Gets the absolute paths to all C# project files.
    /// </summary>
    /// <returns>Enumerable of absolute paths to .csproj files.</returns>
    public IEnumerable<string> GetCSharpProjectPaths()
    {
        return GetCSharpProjects().Select(p => p.GetAbsolutePath(SolutionDirectory));
    }
}
