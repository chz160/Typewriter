using System.Diagnostics;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Typewriter.CLI.Infrastructure.Models;

namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// Discovers source files for a project using glob patterns.
/// </summary>
public class SourceFileDiscovery : ISourceFileDiscovery
{
    /// <summary>
    /// Default include patterns for SDK-style projects.
    /// </summary>
    public static readonly string[] DefaultSdkIncludes = new[] { "**/*.cs" };

    /// <summary>
    /// Default exclude patterns for SDK-style projects.
    /// </summary>
    public static readonly string[] DefaultSdkExcludes = new[]
    {
        "**/obj/**",
        "**/bin/**"
    };

    /// <inheritdoc/>
    public SourceFileResult DiscoverSdkStyleFiles(
        string projectDirectory,
        IEnumerable<string>? includePatterns = null,
        IEnumerable<string>? excludePatterns = null,
        IEnumerable<string>? removePatterns = null)
    {
        var stopwatch = Stopwatch.StartNew();

        // Start with SDK-style defaults
        var allIncludes = DefaultSdkIncludes.ToList();
        var allExcludes = DefaultSdkExcludes.ToList();
        var allRemoves = new List<string>();

        // Add any additional patterns
        if (includePatterns != null)
        {
            allIncludes.AddRange(includePatterns);
        }

        if (excludePatterns != null)
        {
            allExcludes.AddRange(excludePatterns);
        }

        if (removePatterns != null)
        {
            allRemoves.AddRange(removePatterns);
        }

        // Build the matcher
        var matcher = new Matcher();
        foreach (var include in allIncludes)
        {
            matcher.AddInclude(include);
        }
        foreach (var exclude in allExcludes)
        {
            matcher.AddExclude(exclude);
        }

        // Execute the match
        var directoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(projectDirectory));
        var result = matcher.Execute(directoryInfo);

        // Get all matched files as absolute paths
        var includedFiles = result.Files
            .Select(f => Path.GetFullPath(Path.Combine(projectDirectory, f.Path)))
            .ToList();

        // Apply explicit removes
        var removedFiles = new List<string>();
        if (allRemoves.Count > 0)
        {
            var removeMatcher = new Matcher();
            foreach (var remove in allRemoves)
            {
                removeMatcher.AddInclude(remove);
            }
            var removeResult = removeMatcher.Execute(directoryInfo);
            var toRemove = removeResult.Files
                .Select(f => Path.GetFullPath(Path.Combine(projectDirectory, f.Path)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            removedFiles = includedFiles.Where(f => toRemove.Contains(f)).ToList();
            includedFiles = includedFiles.Where(f => !toRemove.Contains(f)).ToList();
        }

        stopwatch.Stop();

        // Build pattern list for reporting
        var patternsUsed = allIncludes
            .Select(p => $"+{p}")
            .Concat(allExcludes.Select(p => $"-{p}"))
            .Concat(allRemoves.Select(p => $"!{p}"))
            .ToList();

        return new SourceFileResult(
            includedFiles,
            removedFiles,
            stopwatch.Elapsed,
            patternsUsed);
    }

    /// <inheritdoc/>
    public SourceFileResult DiscoverLegacyFiles(
        string projectDirectory,
        IEnumerable<string> explicitIncludes)
    {
        var stopwatch = Stopwatch.StartNew();

        var includedFiles = new List<string>();
        var excludedFiles = new List<string>();
        var patternsUsed = new List<string>();

        foreach (var include in explicitIncludes)
        {
            patternsUsed.Add($"+{include}");

            // Handle glob patterns in legacy includes
            if (include.Contains('*') || include.Contains('?'))
            {
                var matcher = new Matcher();
                matcher.AddInclude(include);
                var directoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(projectDirectory));
                var result = matcher.Execute(directoryInfo);

                foreach (var file in result.Files)
                {
                    var fullPath = Path.GetFullPath(Path.Combine(projectDirectory, file.Path));
                    if (File.Exists(fullPath))
                    {
                        includedFiles.Add(fullPath);
                    }
                    else
                    {
                        excludedFiles.Add(fullPath);
                    }
                }
            }
            else
            {
                // Direct file reference
                var fullPath = Path.GetFullPath(Path.Combine(projectDirectory, include));
                if (File.Exists(fullPath))
                {
                    includedFiles.Add(fullPath);
                }
                else
                {
                    excludedFiles.Add(fullPath);
                }
            }
        }

        // Remove duplicates while preserving order
        includedFiles = includedFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        stopwatch.Stop();

        return new SourceFileResult(
            includedFiles,
            excludedFiles,
            stopwatch.Elapsed,
            patternsUsed);
    }
}
