using System.Xml.Linq;
using Typewriter.CLI.Infrastructure.Models;

namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// Parses .csproj files to extract metadata without MSBuild evaluation.
/// Supports both SDK-style and legacy project formats.
/// </summary>
public class ProjectFileParser : IProjectFileParser
{
    private readonly ISourceFileDiscovery _sourceFileDiscovery;

    /// <summary>
    /// MSBuild XML namespace used in legacy project files.
    /// </summary>
    private static readonly XNamespace MsBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectFileParser"/> class.
    /// </summary>
    /// <param name="sourceFileDiscovery">Source file discovery service for glob pattern matching.</param>
    public ProjectFileParser(ISourceFileDiscovery sourceFileDiscovery)
    {
        _sourceFileDiscovery = sourceFileDiscovery ?? throw new ArgumentNullException(nameof(sourceFileDiscovery));
    }

    /// <inheritdoc/>
    public bool IsSdkStyleProject(string projectPath)
    {
        if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
        {
            return false;
        }

        try
        {
            var doc = XDocument.Load(projectPath);
            var projectElement = doc.Root;

            if (projectElement == null)
            {
                return false;
            }

            // SDK-style projects have an Sdk attribute on the Project element
            var sdkAttribute = projectElement.Attribute("Sdk");
            return sdkAttribute != null && !string.IsNullOrEmpty(sdkAttribute.Value);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public ProjectInfo? Parse(string projectPath)
    {
        if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
        {
            return null;
        }

        try
        {
            var fullPath = Path.GetFullPath(projectPath);
            var projectDirectory = Path.GetDirectoryName(fullPath) ?? string.Empty;
            var isSdkStyle = IsSdkStyleProject(fullPath);

            var doc = XDocument.Load(fullPath);
            var projectElement = doc.Root;

            if (projectElement == null)
            {
                return null;
            }

            // Extract common properties
            var targetFramework = GetPropertyValue(projectElement, "TargetFramework", isSdkStyle);
            var assemblyName = GetPropertyValue(projectElement, "AssemblyName", isSdkStyle);
            var rootNamespace = GetPropertyValue(projectElement, "RootNamespace", isSdkStyle);

            // Extract project references
            var projectReferences = GetProjectReferences(projectElement, projectDirectory, isSdkStyle);

            // Extract source files based on project type
            IReadOnlyList<string> sourceFiles;
            IReadOnlyList<string> compileIncludes;
            IReadOnlyList<string> compileExcludes;
            IReadOnlyList<string> compileRemoves;

            if (isSdkStyle)
            {
                var patterns = ExtractSdkStylePatterns(projectElement);
                compileIncludes = patterns.Includes;
                compileExcludes = patterns.Excludes;
                compileRemoves = patterns.Removes;

                // Use glob patterns to discover files
                var result = _sourceFileDiscovery.DiscoverSdkStyleFiles(
                    projectDirectory,
                    compileIncludes.Count > 0 ? compileIncludes : null,
                    compileExcludes.Count > 0 ? compileExcludes : null,
                    compileRemoves.Count > 0 ? compileRemoves : null);

                // Also add explicitly included files from outside the project directory
                var explicitExternalFiles = GetExplicitExternalFiles(projectElement, projectDirectory, isSdkStyle);
                sourceFiles = result.IncludedFiles.Concat(explicitExternalFiles).Distinct().ToList();
            }
            else
            {
                // Legacy projects use explicit Compile includes
                var explicitIncludes = GetLegacyCompileIncludes(projectElement, projectDirectory);
                compileIncludes = explicitIncludes;
                compileExcludes = Array.Empty<string>();
                compileRemoves = Array.Empty<string>();

                var result = _sourceFileDiscovery.DiscoverLegacyFiles(projectDirectory, explicitIncludes);
                sourceFiles = result.IncludedFiles;
            }

            return new ProjectInfo(
                fullPath,
                isSdkStyle,
                targetFramework,
                sourceFiles,
                projectReferences,
                assemblyName,
                rootNamespace,
                compileIncludes,
                compileExcludes,
                compileRemoves);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a property value from the project file.
    /// </summary>
    /// <param name="projectElement">The root Project element.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <param name="isSdkStyle">Whether this is an SDK-style project.</param>
    /// <returns>The property value, or null if not found.</returns>
    private static string? GetPropertyValue(XElement projectElement, string propertyName, bool isSdkStyle)
    {
        if (isSdkStyle)
        {
            // SDK-style: no namespace
            return projectElement
                .Descendants("PropertyGroup")
                .Elements(propertyName)
                .FirstOrDefault()?.Value;
        }
        else
        {
            // Legacy: MSBuild namespace
            return projectElement
                .Descendants(MsBuildNamespace + "PropertyGroup")
                .Elements(MsBuildNamespace + propertyName)
                .FirstOrDefault()?.Value;
        }
    }

    /// <summary>
    /// Extracts project references from the project file.
    /// </summary>
    /// <param name="projectElement">The root Project element.</param>
    /// <param name="projectDirectory">The directory containing the project file.</param>
    /// <param name="isSdkStyle">Whether this is an SDK-style project.</param>
    /// <returns>List of project reference paths.</returns>
    private static List<string> GetProjectReferences(XElement projectElement, string projectDirectory, bool isSdkStyle)
    {
        var references = new List<string>();

        IEnumerable<XElement> projectReferenceElements;

        if (isSdkStyle)
        {
            projectReferenceElements = projectElement
                .Descendants("ItemGroup")
                .Elements("ProjectReference");
        }
        else
        {
            projectReferenceElements = projectElement
                .Descendants(MsBuildNamespace + "ItemGroup")
                .Elements(MsBuildNamespace + "ProjectReference");
        }

        foreach (var element in projectReferenceElements)
        {
            var include = element.Attribute("Include")?.Value;
            if (!string.IsNullOrEmpty(include))
            {
                // Store as relative path for now
                references.Add(include);
            }
        }

        return references;
    }

    /// <summary>
    /// Extracts SDK-style compile patterns (Include, Exclude, Remove).
    /// </summary>
    /// <param name="projectElement">The root Project element.</param>
    /// <returns>Tuple of include, exclude, and remove patterns.</returns>
    private static (IReadOnlyList<string> Includes, IReadOnlyList<string> Excludes, IReadOnlyList<string> Removes) ExtractSdkStylePatterns(XElement projectElement)
    {
        var includes = new List<string>();
        var excludes = new List<string>();
        var removes = new List<string>();

        var compileElements = projectElement
            .Descendants("ItemGroup")
            .Elements("Compile");

        foreach (var element in compileElements)
        {
            var include = element.Attribute("Include")?.Value;
            var exclude = element.Attribute("Exclude")?.Value;
            var remove = element.Attribute("Remove")?.Value;

            if (!string.IsNullOrEmpty(include))
            {
                includes.AddRange(SplitPatterns(include));
            }

            if (!string.IsNullOrEmpty(exclude))
            {
                excludes.AddRange(SplitPatterns(exclude));
            }

            if (!string.IsNullOrEmpty(remove))
            {
                removes.AddRange(SplitPatterns(remove));
            }
        }

        return (includes, excludes, removes);
    }

    /// <summary>
    /// Gets explicitly included files that are outside the project directory.
    /// </summary>
    /// <param name="projectElement">The root Project element.</param>
    /// <param name="projectDirectory">The directory containing the project file.</param>
    /// <param name="isSdkStyle">Whether this is an SDK-style project.</param>
    /// <returns>List of absolute paths to external files.</returns>
    private static List<string> GetExplicitExternalFiles(XElement projectElement, string projectDirectory, bool isSdkStyle)
    {
        var externalFiles = new List<string>();

        IEnumerable<XElement> compileElements;

        if (isSdkStyle)
        {
            compileElements = projectElement
                .Descendants("ItemGroup")
                .Elements("Compile");
        }
        else
        {
            compileElements = projectElement
                .Descendants(MsBuildNamespace + "ItemGroup")
                .Elements(MsBuildNamespace + "Compile");
        }

        foreach (var element in compileElements)
        {
            var include = element.Attribute("Include")?.Value;
            if (!string.IsNullOrEmpty(include) && !ContainsWildcard(include))
            {
                // Check if this is an external file (starts with .. or is absolute)
                if (include.StartsWith("..") || Path.IsPathRooted(include))
                {
                    var fullPath = Path.GetFullPath(Path.Combine(projectDirectory, include));
                    if (File.Exists(fullPath))
                    {
                        externalFiles.Add(fullPath);
                    }
                }
            }
        }

        return externalFiles;
    }

    /// <summary>
    /// Gets legacy Compile include paths.
    /// </summary>
    /// <param name="projectElement">The root Project element.</param>
    /// <param name="projectDirectory">The directory containing the project file.</param>
    /// <returns>List of relative paths from Compile includes.</returns>
    private static List<string> GetLegacyCompileIncludes(XElement projectElement, string projectDirectory)
    {
        var includes = new List<string>();

        var compileElements = projectElement
            .Descendants(MsBuildNamespace + "ItemGroup")
            .Elements(MsBuildNamespace + "Compile");

        foreach (var element in compileElements)
        {
            var include = element.Attribute("Include")?.Value;
            if (!string.IsNullOrEmpty(include))
            {
                includes.Add(include);
            }
        }

        return includes;
    }

    /// <summary>
    /// Splits a semicolon-separated pattern string into individual patterns.
    /// </summary>
    /// <param name="patterns">Semicolon-separated pattern string.</param>
    /// <returns>Individual patterns.</returns>
    private static IEnumerable<string> SplitPatterns(string patterns)
    {
        return patterns.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p));
    }

    /// <summary>
    /// Checks if a path contains wildcard characters.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path contains wildcards.</returns>
    private static bool ContainsWildcard(string path)
    {
        return path.Contains('*') || path.Contains('?');
    }
}
