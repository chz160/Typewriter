using System.Diagnostics;
using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Typewriter.CLI.Infrastructure.Models;

namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// Provides a standalone Roslyn workspace for analyzing solutions and projects
/// without requiring Visual Studio.
/// </summary>
public class CliRoslynWorkspace : IDisposable
{
    private readonly AdhocWorkspace _workspace;
    private readonly AnalyzerManager _analyzerManager;
    private readonly DiagnosticCollection _diagnostics;
    private readonly ConsoleOutput? _output;
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
    /// Initializes a new instance of the <see cref="CliRoslynWorkspace"/> class.
    /// </summary>
    /// <param name="output">Optional console output for logging progress.</param>
    public CliRoslynWorkspace(ConsoleOutput? output = null)
    {
        // Create workspace with explicit C#-only host services to avoid VB assembly version conflicts
        var hostServices = MefHostServices.Create(MefHostServices.DefaultAssemblies
            .Where(a => !a.FullName?.Contains("VisualBasic") ?? true));
        _workspace = new AdhocWorkspace(hostServices);
        _analyzerManager = new AnalyzerManager();
        _diagnostics = new DiagnosticCollection();
        _output = output;
    }

    /// <summary>
    /// Loads a solution file into the workspace.
    /// Tries fast loading first, then falls back to Buildalyzer if needed.
    /// </summary>
    /// <param name="solutionPath">The path to the .sln file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the solution was loaded successfully.</returns>
    public async Task<bool> LoadSolutionAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        var result = await LoadSolutionWithResultAsync(solutionPath, cancellationToken);
        return result.Success;
    }

    /// <summary>
    /// Loads a single project file into the workspace.
    /// Tries fast loading first, then falls back to Buildalyzer if needed.
    /// </summary>
    /// <param name="projectPath">The path to the .csproj file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the project was loaded successfully.</returns>
    public async Task<bool> LoadProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var result = await LoadProjectWithResultAsync(projectPath, cancellationToken);
        return result.Success;
    }

    /// <summary>
    /// Loads a single project file with detailed result information.
    /// Tries fast loading first, then falls back to Buildalyzer if needed.
    /// </summary>
    /// <param name="projectPath">The path to the .csproj file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A LoadingResult with details about the operation.</returns>
    public async Task<LoadingResult> LoadProjectWithResultAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var fullPath = Path.GetFullPath(projectPath);

        if (!File.Exists(fullPath))
        {
            _diagnostics.AddError($"Project file not found: {projectPath}");
            stopwatch.Stop();
            return LoadingResult.Failed(
                _diagnostics.ToModelDiagnostics(),
                stopwatch.Elapsed);
        }

        ProjectPath = fullPath;

        // Try fast loading first
        string? fallbackReason = null;
        try
        {
            _output?.Info($"Loading project (fast): {projectPath}");

            using var fastWorkspace = new DirectRoslynWorkspace(_output);
            var fastResult = await fastWorkspace.LoadProjectAsync(fullPath, cancellationToken);

            if (fastResult && fastWorkspace.GetProjects().Any())
            {
                // Fast loading succeeded - copy the solution to our workspace
                CopyProjectsToWorkspace(fastWorkspace);
                stopwatch.Stop();

                var sourceFileCount = _workspace.CurrentSolution.Projects
                    .SelectMany(p => p.Documents)
                    .Count();

                _output?.Info($"Fast loading completed in {stopwatch.ElapsedMilliseconds}ms");

                return LoadingResult.Successful(
                    stopwatch.Elapsed,
                    _workspace.CurrentSolution.Projects.Count(),
                    sourceFileCount,
                    _diagnostics.ToModelDiagnostics());
            }
            else
            {
                fallbackReason = "Fast loading returned no projects";
            }
        }
        catch (Exception ex)
        {
            fallbackReason = $"Fast loading failed: {ex.Message}";
            _output?.Verbose($"Fast loading exception: {ex.Message}");
        }

        // Fall back to Buildalyzer
        _output?.Warning($"Falling back to Buildalyzer: {fallbackReason}");

        try
        {
            var analyzer = _analyzerManager.GetProject(fullPath);
            var results = analyzer.Build();

            var result = results.FirstOrDefault(r => r.Succeeded);
            if (result != null)
            {
                result.AddToWorkspace(_workspace);
                stopwatch.Stop();

                var sourceFileCount = result.SourceFiles?.Count() ?? 0;
                _output?.Info($"Buildalyzer loading completed in {stopwatch.ElapsedMilliseconds}ms");

                return LoadingResult.SuccessfulWithFallback(
                    fallbackReason!,
                    stopwatch.Elapsed,
                    _workspace.CurrentSolution.Projects.Count(),
                    sourceFileCount,
                    _diagnostics.ToModelDiagnostics());
            }
            else
            {
                _diagnostics.AddError($"Failed to build project with Buildalyzer: {projectPath}");
                stopwatch.Stop();
                return LoadingResult.Failed(
                    _diagnostics.ToModelDiagnostics(),
                    stopwatch.Elapsed);
            }
        }
        catch (Exception ex)
        {
            _diagnostics.AddError($"Buildalyzer failed: {ex.Message}");
            stopwatch.Stop();
            return LoadingResult.Failed(
                _diagnostics.ToModelDiagnostics(),
                stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Loads a solution file with detailed result information.
    /// Tries fast loading first, then falls back to Buildalyzer if needed.
    /// </summary>
    /// <param name="solutionPath">The path to the .sln file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A LoadingResult with details about the operation.</returns>
    public async Task<LoadingResult> LoadSolutionWithResultAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var fullPath = Path.GetFullPath(solutionPath);

        if (!File.Exists(fullPath))
        {
            _diagnostics.AddError($"Solution file not found: {solutionPath}");
            stopwatch.Stop();
            return LoadingResult.Failed(
                _diagnostics.ToModelDiagnostics(),
                stopwatch.Elapsed);
        }

        SolutionPath = fullPath;

        // Try fast loading first
        string? fallbackReason = null;
        try
        {
            _output?.Info($"Loading solution (fast): {solutionPath}");

            using var fastWorkspace = new DirectRoslynWorkspace(_output);
            var fastResult = await fastWorkspace.LoadSolutionAsync(fullPath, cancellationToken);

            if (fastResult && fastWorkspace.GetProjects().Any())
            {
                // Fast loading succeeded - copy the solution to our workspace
                CopyProjectsToWorkspace(fastWorkspace);
                stopwatch.Stop();

                var sourceFileCount = _workspace.CurrentSolution.Projects
                    .SelectMany(p => p.Documents)
                    .Count();

                _output?.Info($"Fast loading completed in {stopwatch.ElapsedMilliseconds}ms");

                return LoadingResult.Successful(
                    stopwatch.Elapsed,
                    _workspace.CurrentSolution.Projects.Count(),
                    sourceFileCount,
                    _diagnostics.ToModelDiagnostics());
            }
            else
            {
                fallbackReason = "Fast loading returned no projects";
            }
        }
        catch (Exception ex)
        {
            fallbackReason = $"Fast loading failed: {ex.Message}";
            _output?.Verbose($"Fast loading exception: {ex.Message}");
        }

        // Fall back to Buildalyzer
        _output?.Warning($"Falling back to Buildalyzer: {fallbackReason}");

        try
        {
            var manager = new AnalyzerManager(fullPath);
            var projectCount = 0;
            var totalSourceFiles = 0;

            foreach (var project in manager.Projects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _output?.Verbose($"  Loading project: {project.Key}");
                    var analyzer = project.Value;
                    var results = analyzer.Build();

                    var result = results.FirstOrDefault(r => r.Succeeded);
                    if (result != null)
                    {
                        result.AddToWorkspace(_workspace);
                        projectCount++;
                        totalSourceFiles += result.SourceFiles?.Count() ?? 0;
                    }
                    else
                    {
                        _diagnostics.AddWarning($"Failed to load project: {project.Key}");
                    }
                }
                catch (Exception ex)
                {
                    _diagnostics.AddWarning($"Error loading project {project.Key}: {ex.Message}");
                }
            }

            stopwatch.Stop();
            _output?.Info($"Buildalyzer loading completed in {stopwatch.ElapsedMilliseconds}ms");

            if (projectCount > 0)
            {
                return LoadingResult.SuccessfulWithFallback(
                    fallbackReason!,
                    stopwatch.Elapsed,
                    projectCount,
                    totalSourceFiles,
                    _diagnostics.ToModelDiagnostics());
            }
            else
            {
                _diagnostics.AddError($"No projects could be loaded from solution: {solutionPath}");
                return LoadingResult.Failed(
                    _diagnostics.ToModelDiagnostics(),
                    stopwatch.Elapsed);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _diagnostics.AddError($"Buildalyzer failed: {ex.Message}");
            stopwatch.Stop();
            return LoadingResult.Failed(
                _diagnostics.ToModelDiagnostics(),
                stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Copies projects from a DirectRoslynWorkspace to this workspace.
    /// </summary>
    /// <param name="sourceWorkspace">The source workspace to copy from.</param>
    private void CopyProjectsToWorkspace(DirectRoslynWorkspace sourceWorkspace)
    {
        // We need to transfer the solution from the DirectRoslynWorkspace to our AdhocWorkspace
        // The simplest approach is to add each project and its documents
        foreach (var project in sourceWorkspace.GetProjects())
        {
            var projectInfo = Microsoft.CodeAnalysis.ProjectInfo.Create(
                ProjectId.CreateNewId(project.Name),
                VersionStamp.Create(),
                project.Name,
                project.AssemblyName,
                project.Language,
                filePath: project.FilePath,
                compilationOptions: project.CompilationOptions,
                parseOptions: project.ParseOptions,
                metadataReferences: project.MetadataReferences);

            var solution = _workspace.CurrentSolution.AddProject(projectInfo);

            foreach (var document in project.Documents)
            {
                var documentId = DocumentId.CreateNewId(projectInfo.Id, document.Name);
                var text = document.GetTextAsync().Result;
                solution = solution.AddDocument(documentId, document.Name, text, filePath: document.FilePath);
            }

            _workspace.TryApplyChanges(solution);
        }
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
