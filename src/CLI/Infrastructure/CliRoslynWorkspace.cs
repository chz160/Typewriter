using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;

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
        _workspace = new AdhocWorkspace();
        _analyzerManager = new AnalyzerManager();
        _diagnostics = new DiagnosticCollection();
        _output = output;
    }

    /// <summary>
    /// Loads a solution file into the workspace.
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
            _output?.Info($"Loading solution: {solutionPath}");

            // Use Buildalyzer to analyze all projects in the solution
            var manager = new AnalyzerManager(fullPath);

            foreach (var project in manager.Projects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _output?.Verbose($"  Loading project: {project.Key}");
                    var analyzer = project.Value;
                    var results = analyzer.Build();

                    // Get the first successful build result
                    var result = results.FirstOrDefault(r => r.Succeeded);
                    if (result != null)
                    {
                        result.AddToWorkspace(_workspace);
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
    /// Loads a single project file into the workspace.
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
            _output?.Info($"Loading project: {projectPath}");

            var analyzer = _analyzerManager.GetProject(fullPath);
            var results = analyzer.Build();

            var result = results.FirstOrDefault(r => r.Succeeded);
            if (result != null)
            {
                result.AddToWorkspace(_workspace);
                _output?.Info($"Loaded project with {result.SourceFiles?.Count() ?? 0} source file(s)");
                return true;
            }
            else
            {
                _diagnostics.AddError($"Failed to build project: {projectPath}");
                return false;
            }
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
