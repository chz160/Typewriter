using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// Provides a fast Roslyn workspace that loads projects directly without MSBuild evaluation.
/// Uses XML parsing and glob patterns instead of Buildalyzer for significantly faster loading.
/// </summary>
public class DirectRoslynWorkspace : IDisposable
{
    private readonly AdhocWorkspace _workspace;
    private readonly IProjectFileParser _projectFileParser;
    private readonly ISolutionFileParser _solutionFileParser;
    private readonly IReferenceResolver _referenceResolver;
    private readonly DiagnosticCollection _diagnostics;
    private readonly ConsoleOutput? _output;
    private readonly Dictionary<string, ProjectId> _projectPathToId = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    /// <summary>
    /// Gets the underlying Roslyn workspace.
    /// </summary>
    public Workspace Workspace => _workspace;

    /// <summary>
    /// Gets the current solution in the workspace.
    /// </summary>
    public Solution Solution => _workspace.CurrentSolution;

    /// <summary>
    /// Gets the diagnostics collection for reporting errors and warnings.
    /// </summary>
    public DiagnosticCollection Diagnostics => _diagnostics;

    /// <summary>
    /// Gets the path to the loaded solution, or null if a project was loaded.
    /// </summary>
    public string? SolutionPath { get; private set; }

    /// <summary>
    /// Gets the path to the loaded project, or null if a solution was loaded.
    /// </summary>
    public string? ProjectPath { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectRoslynWorkspace"/> class.
    /// </summary>
    /// <param name="output">Optional console output for logging progress.</param>
    public DirectRoslynWorkspace(ConsoleOutput? output = null)
        : this(
            new ProjectFileParser(new SourceFileDiscovery()),
            new SolutionFileParser(),
            new ReferenceResolver(),
            output)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectRoslynWorkspace"/> class with custom dependencies.
    /// </summary>
    /// <param name="projectFileParser">The project file parser to use.</param>
    /// <param name="solutionFileParser">The solution file parser to use.</param>
    /// <param name="referenceResolver">The reference resolver to use.</param>
    /// <param name="output">Optional console output for logging progress.</param>
    public DirectRoslynWorkspace(
        IProjectFileParser projectFileParser,
        ISolutionFileParser solutionFileParser,
        IReferenceResolver referenceResolver,
        ConsoleOutput? output = null)
    {
        // Create workspace with explicit C#-only host services to avoid VB assembly version conflicts
        var hostServices = MefHostServices.Create(MefHostServices.DefaultAssemblies
            .Where(a => !a.FullName?.Contains("VisualBasic") ?? true));
        _workspace = new AdhocWorkspace(hostServices);
        _projectFileParser = projectFileParser ?? throw new ArgumentNullException(nameof(projectFileParser));
        _solutionFileParser = solutionFileParser ?? throw new ArgumentNullException(nameof(solutionFileParser));
        _referenceResolver = referenceResolver ?? throw new ArgumentNullException(nameof(referenceResolver));
        _diagnostics = new DiagnosticCollection();
        _output = output;
    }

    /// <summary>
    /// Loads a single project file into the workspace using direct parsing.
    /// </summary>
    /// <param name="projectPath">The path to the .csproj file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the project was loaded successfully.</returns>
    public async Task<bool> LoadProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.GetFullPath(projectPath);

            if (!File.Exists(fullPath))
            {
                _diagnostics.AddError($"Project file not found: {projectPath}");
                return false;
            }

            ProjectPath = fullPath;
            _output?.Info($"Loading project (fast): {projectPath}");

            // Parse the project file
            var projectInfo = _projectFileParser.Parse(fullPath);
            if (projectInfo == null)
            {
                _diagnostics.AddError($"Failed to parse project file: {projectPath}");
                return false;
            }

            // Create the project in the workspace
            var roslynProject = await CreateProjectAsync(projectInfo, cancellationToken);
            if (roslynProject == null)
            {
                _diagnostics.AddError($"Failed to create Roslyn project: {projectPath}");
                return false;
            }

            _output?.Info($"Loaded project with {projectInfo.SourceFiles.Count} source file(s)");
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _diagnostics.AddError($"Failed to load project: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Loads a solution file into the workspace using direct parsing.
    /// Projects are loaded in topological order to resolve project references.
    /// </summary>
    /// <param name="solutionPath">The path to the .sln file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the solution was loaded successfully.</returns>
    public async Task<bool> LoadSolutionAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.GetFullPath(solutionPath);

            if (!File.Exists(fullPath))
            {
                _diagnostics.AddError($"Solution file not found: {solutionPath}");
                return false;
            }

            SolutionPath = fullPath;
            _output?.Info($"Loading solution (fast): {solutionPath}");

            // Parse the solution file
            var solutionInfo = _solutionFileParser.Parse(fullPath);
            if (solutionInfo == null)
            {
                _diagnostics.AddError($"Failed to parse solution file: {solutionPath}");
                return false;
            }

            // Get all C# projects and parse them
            var projectPaths = solutionInfo.GetCSharpProjectPaths().ToList();
            var parsedProjects = new Dictionary<string, Models.ProjectInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var projectPath in projectPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var projectInfo = _projectFileParser.Parse(projectPath);
                if (projectInfo != null)
                {
                    parsedProjects[projectPath] = projectInfo;
                }
                else
                {
                    _diagnostics.AddWarning($"Failed to parse project: {projectPath}");
                }
            }

            // Sort projects topologically by dependencies
            var sortedProjects = TopologicalSort(parsedProjects);

            // Load projects in order
            foreach (var projectInfo in sortedProjects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _output?.Verbose($"  Loading project: {projectInfo.ProjectName}");
                var project = await CreateProjectWithReferencesAsync(projectInfo, cancellationToken);
                if (project == null)
                {
                    _diagnostics.AddWarning($"Failed to load project: {projectInfo.ProjectPath}");
                }
            }

            _output?.Info($"Loaded {_workspace.CurrentSolution.Projects.Count()} project(s)");
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _diagnostics.AddError($"Failed to load solution: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sorts projects topologically based on project references.
    /// Projects with no dependencies come first.
    /// </summary>
    /// <param name="projects">Dictionary of project paths to project info.</param>
    /// <returns>Projects sorted in dependency order.</returns>
    private static List<Models.ProjectInfo> TopologicalSort(Dictionary<string, Models.ProjectInfo> projects)
    {
        var result = new List<Models.ProjectInfo>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Visit(Models.ProjectInfo project)
        {
            if (visited.Contains(project.ProjectPath))
            {
                return;
            }

            if (visiting.Contains(project.ProjectPath))
            {
                // Circular dependency - just add it
                return;
            }

            visiting.Add(project.ProjectPath);

            // Visit dependencies first
            foreach (var refPath in project.ProjectReferences)
            {
                var absoluteRefPath = Path.GetFullPath(Path.Combine(project.ProjectDirectory, refPath));
                if (projects.TryGetValue(absoluteRefPath, out var refProject))
                {
                    Visit(refProject);
                }
            }

            visiting.Remove(project.ProjectPath);
            visited.Add(project.ProjectPath);
            result.Add(project);
        }

        foreach (var project in projects.Values)
        {
            Visit(project);
        }

        return result;
    }

    /// <summary>
    /// Creates a Roslyn project with project references resolved.
    /// </summary>
    /// <param name="projectInfo">The parsed project information.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created Roslyn project.</returns>
    private async Task<Project?> CreateProjectWithReferencesAsync(Models.ProjectInfo projectInfo, CancellationToken cancellationToken)
    {
        var projectId = ProjectId.CreateNewId(projectInfo.ProjectName);
        _projectPathToId[projectInfo.ProjectPath] = projectId;

        // Get runtime references
        var metadataReferences = _referenceResolver.GetRuntimeReferences().ToList();

        // Resolve project references
        var projectReferences = new List<ProjectReference>();
        foreach (var refPath in projectInfo.ProjectReferences)
        {
            var absoluteRefPath = Path.GetFullPath(Path.Combine(projectInfo.ProjectDirectory, refPath));
            if (_projectPathToId.TryGetValue(absoluteRefPath, out var refProjectId))
            {
                projectReferences.Add(new ProjectReference(refProjectId));
            }
        }

        // Determine language version based on target framework
        var languageVersion = GetLanguageVersion(projectInfo.TargetFramework);

        // Create compilation options
        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable);

        var parseOptions = new CSharpParseOptions(languageVersion);

        // Create the project info
        var roslynProjectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            projectInfo.ProjectName,
            projectInfo.AssemblyName ?? projectInfo.ProjectName,
            LanguageNames.CSharp,
            filePath: projectInfo.ProjectPath,
            compilationOptions: compilationOptions,
            parseOptions: parseOptions,
            metadataReferences: metadataReferences,
            projectReferences: projectReferences);

        // Add the project to the workspace
        var solution = _workspace.CurrentSolution.AddProject(roslynProjectInfo);

        // Add source files as documents
        foreach (var sourceFile in projectInfo.SourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(sourceFile))
            {
                var documentId = DocumentId.CreateNewId(projectId, sourceFile);
                var fileName = Path.GetFileName(sourceFile);

                try
                {
                    var sourceText = SourceText.From(
                        await File.ReadAllTextAsync(sourceFile, cancellationToken),
                        System.Text.Encoding.UTF8);

                    solution = solution.AddDocument(documentId, fileName, sourceText, filePath: sourceFile);
                }
                catch (Exception ex)
                {
                    _diagnostics.AddWarning($"Failed to load source file {sourceFile}: {ex.Message}");
                }
            }
            else
            {
                _diagnostics.AddWarning($"Source file not found: {sourceFile}");
            }
        }

        // Apply the solution changes
        if (!_workspace.TryApplyChanges(solution))
        {
            _diagnostics.AddWarning("Failed to apply solution changes");
            return null;
        }

        return _workspace.CurrentSolution.GetProject(projectId);
    }

    /// <summary>
    /// Creates a Roslyn project from parsed project info.
    /// </summary>
    /// <param name="projectInfo">The parsed project information.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created Roslyn project.</returns>
    private async Task<Project?> CreateProjectAsync(Models.ProjectInfo projectInfo, CancellationToken cancellationToken)
    {
        var projectId = ProjectId.CreateNewId(projectInfo.ProjectName);

        // Get runtime references
        var references = _referenceResolver.GetRuntimeReferences().ToList();

        // Determine language version based on target framework
        var languageVersion = GetLanguageVersion(projectInfo.TargetFramework);

        // Create compilation options
        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable);

        var parseOptions = new CSharpParseOptions(languageVersion);

        // Create the project info
        var roslynProjectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            projectInfo.ProjectName,
            projectInfo.AssemblyName ?? projectInfo.ProjectName,
            LanguageNames.CSharp,
            filePath: projectInfo.ProjectPath,
            compilationOptions: compilationOptions,
            parseOptions: parseOptions,
            metadataReferences: references);

        // Add the project to the workspace
        var solution = _workspace.CurrentSolution.AddProject(roslynProjectInfo);

        // Add source files as documents
        foreach (var sourceFile in projectInfo.SourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(sourceFile))
            {
                var documentId = DocumentId.CreateNewId(projectId, sourceFile);
                var fileName = Path.GetFileName(sourceFile);

                try
                {
                    var sourceText = SourceText.From(
                        await File.ReadAllTextAsync(sourceFile, cancellationToken),
                        System.Text.Encoding.UTF8);

                    solution = solution.AddDocument(documentId, fileName, sourceText, filePath: sourceFile);
                }
                catch (Exception ex)
                {
                    _diagnostics.AddWarning($"Failed to load source file {sourceFile}: {ex.Message}");
                }
            }
            else
            {
                _diagnostics.AddWarning($"Source file not found: {sourceFile}");
            }
        }

        // Apply the solution changes
        if (!_workspace.TryApplyChanges(solution))
        {
            _diagnostics.AddWarning("Failed to apply solution changes");
            return null;
        }

        return _workspace.CurrentSolution.GetProject(projectId);
    }

    /// <summary>
    /// Gets the appropriate C# language version for a target framework.
    /// </summary>
    /// <param name="targetFramework">The target framework moniker.</param>
    /// <returns>The appropriate language version.</returns>
    private static LanguageVersion GetLanguageVersion(string? targetFramework)
    {
        if (string.IsNullOrEmpty(targetFramework))
        {
            return LanguageVersion.Default;
        }

        // Extract version number from target framework
        if (targetFramework.StartsWith("net8"))
        {
            return LanguageVersion.CSharp12;
        }
        if (targetFramework.StartsWith("net7"))
        {
            return LanguageVersion.CSharp11;
        }
        if (targetFramework.StartsWith("net6"))
        {
            return LanguageVersion.CSharp10;
        }
        if (targetFramework.StartsWith("net5"))
        {
            return LanguageVersion.CSharp9;
        }
        if (targetFramework.StartsWith("netcoreapp3"))
        {
            return LanguageVersion.CSharp8;
        }
        if (targetFramework.StartsWith("netstandard2.1"))
        {
            return LanguageVersion.CSharp8;
        }
        if (targetFramework.Contains("4.7") || targetFramework.Contains("4.8"))
        {
            return LanguageVersion.CSharp7_3;
        }

        return LanguageVersion.Default;
    }

    /// <summary>
    /// Gets all projects in the loaded solution.
    /// </summary>
    /// <returns>An enumerable of projects.</returns>
    public IEnumerable<Project> GetProjects()
    {
        return _workspace.CurrentSolution.Projects;
    }

    /// <summary>
    /// Gets all documents (source files) across all projects.
    /// </summary>
    /// <returns>An enumerable of documents.</returns>
    public IEnumerable<Document> GetAllDocuments()
    {
        return _workspace.CurrentSolution.Projects.SelectMany(p => p.Documents);
    }

    /// <summary>
    /// Gets the compilation for a project.
    /// </summary>
    /// <param name="project">The project to get compilation for.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The compilation, or null if it could not be obtained.</returns>
    public async Task<Compilation?> GetCompilationAsync(Project project, CancellationToken cancellationToken = default)
    {
        try
        {
            return await project.GetCompilationAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _diagnostics.AddWarning($"Failed to get compilation for {project.Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Disposes of the workspace and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _workspace.Dispose();
            _disposed = true;
        }
    }
}
