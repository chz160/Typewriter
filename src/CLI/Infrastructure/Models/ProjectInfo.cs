namespace Typewriter.CLI.Infrastructure.Models;

/// <summary>
/// Represents a parsed .csproj file with its metadata and source files.
/// </summary>
public sealed class ProjectInfo
{
    /// <summary>
    /// Gets the absolute path to the .csproj file.
    /// </summary>
    public string ProjectPath { get; }

    /// <summary>
    /// Gets the directory containing the .csproj file.
    /// </summary>
    public string ProjectDirectory { get; }

    /// <summary>
    /// Gets the project name derived from the file name.
    /// </summary>
    public string ProjectName { get; }

    /// <summary>
    /// Gets a value indicating whether this is an SDK-style project.
    /// </summary>
    public bool IsSdkStyle { get; }

    /// <summary>
    /// Gets the target framework moniker (e.g., "net8.0").
    /// </summary>
    public string? TargetFramework { get; }

    /// <summary>
    /// Gets the absolute paths to discovered .cs source files.
    /// </summary>
    public IReadOnlyList<string> SourceFiles { get; }

    /// <summary>
    /// Gets the relative paths to referenced .csproj files.
    /// </summary>
    public IReadOnlyList<string> ProjectReferences { get; }

    /// <summary>
    /// Gets the output assembly name.
    /// </summary>
    public string? AssemblyName { get; }

    /// <summary>
    /// Gets the default root namespace.
    /// </summary>
    public string? RootNamespace { get; }

    /// <summary>
    /// Gets additional compile include patterns from the project file.
    /// </summary>
    public IReadOnlyList<string> CompileIncludes { get; }

    /// <summary>
    /// Gets compile exclude patterns from the project file.
    /// </summary>
    public IReadOnlyList<string> CompileExcludes { get; }

    /// <summary>
    /// Gets compile remove patterns from the project file.
    /// </summary>
    public IReadOnlyList<string> CompileRemoves { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectInfo"/> class.
    /// </summary>
    /// <param name="projectPath">Absolute path to the .csproj file.</param>
    /// <param name="isSdkStyle">Whether this is an SDK-style project.</param>
    /// <param name="targetFramework">Target framework moniker.</param>
    /// <param name="sourceFiles">Discovered source files.</param>
    /// <param name="projectReferences">Project references.</param>
    /// <param name="assemblyName">Output assembly name.</param>
    /// <param name="rootNamespace">Default root namespace.</param>
    /// <param name="compileIncludes">Additional compile includes.</param>
    /// <param name="compileExcludes">Compile excludes.</param>
    /// <param name="compileRemoves">Compile removes.</param>
    public ProjectInfo(
        string projectPath,
        bool isSdkStyle,
        string? targetFramework = null,
        IReadOnlyList<string>? sourceFiles = null,
        IReadOnlyList<string>? projectReferences = null,
        string? assemblyName = null,
        string? rootNamespace = null,
        IReadOnlyList<string>? compileIncludes = null,
        IReadOnlyList<string>? compileExcludes = null,
        IReadOnlyList<string>? compileRemoves = null)
    {
        ProjectPath = Path.GetFullPath(projectPath);
        ProjectDirectory = Path.GetDirectoryName(ProjectPath) ?? string.Empty;
        ProjectName = Path.GetFileNameWithoutExtension(ProjectPath);
        IsSdkStyle = isSdkStyle;
        TargetFramework = targetFramework;
        SourceFiles = sourceFiles ?? Array.Empty<string>();
        ProjectReferences = projectReferences ?? Array.Empty<string>();
        AssemblyName = assemblyName ?? ProjectName;
        RootNamespace = rootNamespace ?? ProjectName;
        CompileIncludes = compileIncludes ?? Array.Empty<string>();
        CompileExcludes = compileExcludes ?? Array.Empty<string>();
        CompileRemoves = compileRemoves ?? Array.Empty<string>();
    }

    /// <summary>
    /// Creates a new ProjectInfo with updated source files.
    /// </summary>
    /// <param name="sourceFiles">The new list of source files.</param>
    /// <returns>A new ProjectInfo instance with updated source files.</returns>
    public ProjectInfo WithSourceFiles(IReadOnlyList<string> sourceFiles)
    {
        return new ProjectInfo(
            ProjectPath,
            IsSdkStyle,
            TargetFramework,
            sourceFiles,
            ProjectReferences,
            AssemblyName,
            RootNamespace,
            CompileIncludes,
            CompileExcludes,
            CompileRemoves);
    }
}
