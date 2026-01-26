using Microsoft.CodeAnalysis;
using Typewriter.CLI.Infrastructure.Models;

namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// Resolves assembly references for Roslyn compilation.
/// </summary>
public interface IReferenceResolver
{
    /// <summary>
    /// Gets .NET runtime assembly references from the current runtime.
    /// </summary>
    /// <returns>Enumerable of MetadataReference for runtime assemblies.</returns>
    IEnumerable<MetadataReference> GetRuntimeReferences();

    /// <summary>
    /// Gets assembly references from a project's reference assemblies.
    /// </summary>
    /// <param name="projectInfo">Project to get references for.</param>
    /// <returns>Enumerable of MetadataReference.</returns>
    IEnumerable<MetadataReference> GetProjectReferences(Models.ProjectInfo projectInfo);
}
