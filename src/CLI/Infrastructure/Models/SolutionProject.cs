namespace Typewriter.CLI.Infrastructure.Models;

/// <summary>
/// Represents a project entry in a solution file.
/// </summary>
public sealed class SolutionProject
{
    /// <summary>
    /// Legacy C# project type GUID (used by older .csproj format).
    /// </summary>
    public static readonly Guid CSharpProjectTypeGuid = new("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");

    /// <summary>
    /// SDK-style C# project type GUID (used by modern .NET Core/5/6/7/8+ projects).
    /// </summary>
    public static readonly Guid SdkStyleCSharpProjectTypeGuid = new("9A19103F-16F7-4668-BE54-9A1E7A4F7556");

    /// <summary>
    /// Solution folder type GUID.
    /// </summary>
    public static readonly Guid SolutionFolderTypeGuid = new("2150E333-8FDC-42A3-9474-1A3956D46DE8");

    /// <summary>
    /// Gets the unique project identifier.
    /// </summary>
    public Guid ProjectGuid { get; }

    /// <summary>
    /// Gets the display name in the solution.
    /// </summary>
    public string ProjectName { get; }

    /// <summary>
    /// Gets the relative path from solution to .csproj.
    /// </summary>
    public string RelativePath { get; }

    /// <summary>
    /// Gets the type identifier (C# project, folder, etc.).
    /// </summary>
    public Guid ProjectTypeGuid { get; }

    /// <summary>
    /// Gets a value indicating whether this is a C# project (legacy or SDK-style).
    /// </summary>
    public bool IsCSharpProject => ProjectTypeGuid == CSharpProjectTypeGuid || ProjectTypeGuid == SdkStyleCSharpProjectTypeGuid;

    /// <summary>
    /// Gets a value indicating whether this is a solution folder.
    /// </summary>
    public bool IsSolutionFolder => ProjectTypeGuid == SolutionFolderTypeGuid;

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionProject"/> class.
    /// </summary>
    /// <param name="projectGuid">Unique project identifier.</param>
    /// <param name="projectName">Display name in solution.</param>
    /// <param name="relativePath">Relative path from solution to project.</param>
    /// <param name="projectTypeGuid">Project type identifier.</param>
    public SolutionProject(Guid projectGuid, string projectName, string relativePath, Guid projectTypeGuid)
    {
        ProjectGuid = projectGuid;
        ProjectName = projectName;
        RelativePath = relativePath;
        ProjectTypeGuid = projectTypeGuid;
    }

    /// <summary>
    /// Gets the absolute path to the project file given a solution directory.
    /// </summary>
    /// <param name="solutionDirectory">The directory containing the solution file.</param>
    /// <returns>Absolute path to the project file.</returns>
    public string GetAbsolutePath(string solutionDirectory)
    {
        var combinedPath = Path.Combine(solutionDirectory, RelativePath);
        return Path.GetFullPath(combinedPath);
    }
}
