namespace Typewriter.CLI.Infrastructure.Models;

/// <summary>
/// Result of source file discovery for a project.
/// </summary>
public sealed class SourceFileResult
{
    /// <summary>
    /// Gets the files matching include patterns.
    /// </summary>
    public IReadOnlyList<string> IncludedFiles { get; }

    /// <summary>
    /// Gets the files that were removed by explicit Remove patterns.
    /// </summary>
    public IReadOnlyList<string> RemovedFiles { get; }

    /// <summary>
    /// Gets the time taken for discovery.
    /// </summary>
    public TimeSpan DiscoveryTime { get; }

    /// <summary>
    /// Gets the patterns that were applied (+ for include, - for exclude, ! for remove).
    /// </summary>
    public IReadOnlyList<string> PatternsUsed { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SourceFileResult"/> class.
    /// </summary>
    /// <param name="includedFiles">Files that matched include patterns.</param>
    /// <param name="removedFiles">Files that were removed by explicit Remove patterns.</param>
    /// <param name="discoveryTime">Time taken for discovery.</param>
    /// <param name="patternsUsed">Patterns that were applied.</param>
    public SourceFileResult(
        IReadOnlyList<string> includedFiles,
        IReadOnlyList<string> removedFiles,
        TimeSpan discoveryTime,
        IReadOnlyList<string> patternsUsed)
    {
        IncludedFiles = includedFiles;
        RemovedFiles = removedFiles;
        DiscoveryTime = discoveryTime;
        PatternsUsed = patternsUsed;
    }

    /// <summary>
    /// Creates an empty result with no files.
    /// </summary>
    /// <returns>An empty source file result.</returns>
    public static SourceFileResult Empty => new(
        Array.Empty<string>(),
        Array.Empty<string>(),
        TimeSpan.Zero,
        Array.Empty<string>());
}
