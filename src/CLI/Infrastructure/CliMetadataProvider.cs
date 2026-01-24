using Microsoft.CodeAnalysis;
using Typewriter.CLI.CodeModel.Implementation;
using Typewriter.CLI.Configuration;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;
using Typewriter.Metadata.Providers;

namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// Provides metadata for C# files using a standalone Roslyn workspace.
/// This is the CLI implementation of <see cref="IMetadataProvider"/> that works
/// without Visual Studio dependencies.
/// </summary>
public class CliMetadataProvider : IMetadataProvider
{
    private readonly CliRoslynWorkspace _workspace;
    private readonly DiagnosticCollection _diagnostics;

    /// <summary>
    /// Initializes a new instance of the <see cref="CliMetadataProvider"/> class.
    /// </summary>
    /// <param name="workspace">The Roslyn workspace containing the loaded solution/project.</param>
    public CliMetadataProvider(CliRoslynWorkspace workspace)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _diagnostics = workspace.Diagnostics;
    }

    /// <summary>
    /// Gets the file metadata for a C# source file.
    /// </summary>
    /// <param name="path">The path to the C# file.</param>
    /// <param name="settings">The generation settings.</param>
    /// <param name="requestRender">Callback for requesting additional renders (used for partial classes).</param>
    /// <returns>The file metadata, or null if the file is not found.</returns>
    public IFileMetadata? GetFile(string path, Settings settings, Action<string[]> requestRender)
    {
        var documentIds = _workspace.Solution.GetDocumentIdsWithFilePath(path);
        var documentId = documentIds.FirstOrDefault();

        if (documentId == null)
        {
            _diagnostics.AddWarning($"Document not found in workspace: {path}");
            return null;
        }

        var document = _workspace.Solution.GetDocument(documentId);
        if (document == null)
        {
            _diagnostics.AddWarning($"Could not load document: {path}");
            return null;
        }

        try
        {
            // Create a CLI-specific file metadata that doesn't depend on VS threading
            return new CliFileMetadata(document, settings, requestRender);
        }
        catch (Exception ex)
        {
            _diagnostics.AddError($"Error getting metadata for {path}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets all documents in the workspace.
    /// </summary>
    /// <returns>An enumerable of all documents.</returns>
    public IEnumerable<Document> GetAllDocuments()
    {
        return _workspace.GetAllDocuments();
    }

    /// <summary>
    /// Gets file metadata for all C# files in the workspace.
    /// </summary>
    /// <param name="settings">The generation settings.</param>
    /// <param name="requestRender">Callback for requesting additional renders.</param>
    /// <returns>An enumerable of file metadata objects.</returns>
    public IEnumerable<IFileMetadata> GetAllFiles(Settings settings, Action<string[]> requestRender)
    {
        foreach (var document in GetAllDocuments())
        {
            if (document.FilePath != null && document.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                var metadata = GetFile(document.FilePath, settings, requestRender);
                if (metadata != null)
                {
                    yield return metadata;
                }
            }
        }
    }

    /// <summary>
    /// Gets the paths of all C# source files in the workspace.
    /// </summary>
    /// <returns>A list of source file paths.</returns>
    public IReadOnlyList<string> GetAllSourceFiles()
    {
        return GetAllDocuments()
            .Where(d => d.FilePath != null && d.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Select(d => d.FilePath!)
            .Distinct()
            .OrderBy(p => p)
            .ToList();
    }

    /// <summary>
    /// Gets file metadata for a C# source file without settings.
    /// </summary>
    /// <param name="path">The path to the C# file.</param>
    /// <returns>The file metadata, or null if the file is not found.</returns>
    public IFileMetadata? GetFileMetadata(string path)
    {
        var solutionPath = _workspace.SolutionPath ?? Path.GetDirectoryName(path) ?? string.Empty;
        return GetFile(path, new CliSettings(solutionPath, path), _ => { });
    }
}
