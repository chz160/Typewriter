using Microsoft.CodeAnalysis;
using Typewriter.CLI.Infrastructure.Models;

namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// Resolves assembly references for Roslyn compilation.
/// </summary>
public class ReferenceResolver : IReferenceResolver
{
    private readonly Lazy<IReadOnlyList<MetadataReference>> _runtimeReferences;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceResolver"/> class.
    /// </summary>
    public ReferenceResolver()
    {
        _runtimeReferences = new Lazy<IReadOnlyList<MetadataReference>>(LoadRuntimeReferences);
    }

    /// <inheritdoc/>
    public IEnumerable<MetadataReference> GetRuntimeReferences()
    {
        return _runtimeReferences.Value;
    }

    /// <inheritdoc/>
    public IEnumerable<MetadataReference> GetProjectReferences(Models.ProjectInfo projectInfo)
    {
        // Return runtime references - project-specific references will be handled
        // by the workspace when resolving ProjectReferences
        return GetRuntimeReferences();
    }

    /// <summary>
    /// Loads runtime assembly references from the current .NET runtime.
    /// </summary>
    /// <returns>List of metadata references for runtime assemblies.</returns>
    private static IReadOnlyList<MetadataReference> LoadRuntimeReferences()
    {
        var references = new List<MetadataReference>();

        // Get runtime assemblies from TRUSTED_PLATFORM_ASSEMBLIES
        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;

        if (!string.IsNullOrEmpty(trustedAssemblies))
        {
            var paths = trustedAssemblies.Split(Path.PathSeparator);

            foreach (var path in paths)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    try
                    {
                        references.Add(MetadataReference.CreateFromFile(path));
                    }
                    catch
                    {
                        // Skip assemblies that can't be loaded
                    }
                }
            }
        }

        // Fallback: If no trusted assemblies found, try to load core assemblies
        if (references.Count == 0)
        {
            var coreAssemblyPath = typeof(object).Assembly.Location;
            var coreDirectory = Path.GetDirectoryName(coreAssemblyPath);

            if (!string.IsNullOrEmpty(coreDirectory))
            {
                var coreAssemblies = new[]
                {
                    "System.Runtime.dll",
                    "System.Collections.dll",
                    "System.Linq.dll",
                    "System.Console.dll",
                    "netstandard.dll",
                    "mscorlib.dll"
                };

                foreach (var assembly in coreAssemblies)
                {
                    var assemblyPath = Path.Combine(coreDirectory, assembly);
                    if (File.Exists(assemblyPath))
                    {
                        try
                        {
                            references.Add(MetadataReference.CreateFromFile(assemblyPath));
                        }
                        catch
                        {
                            // Skip assemblies that can't be loaded
                        }
                    }
                }
            }
        }

        return references;
    }

    /// <summary>
    /// Gets the count of available runtime references.
    /// </summary>
    /// <returns>Number of runtime references loaded.</returns>
    public int GetRuntimeReferenceCount()
    {
        return _runtimeReferences.Value.Count;
    }
}
