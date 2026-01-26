namespace Typewriter.CLI.Infrastructure.Models;

/// <summary>
/// Result of a workspace loading operation.
/// </summary>
public sealed class LoadingResult
{
    /// <summary>
    /// Gets a value indicating whether loading completed successfully.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets a value indicating whether Buildalyzer fallback was used.
    /// </summary>
    public bool UsedFallback { get; }

    /// <summary>
    /// Gets the reason why fallback was triggered, if applicable.
    /// </summary>
    public string? FallbackReason { get; }

    /// <summary>
    /// Gets the total loading time.
    /// </summary>
    public TimeSpan LoadTime { get; }

    /// <summary>
    /// Gets the number of projects loaded.
    /// </summary>
    public int ProjectCount { get; }

    /// <summary>
    /// Gets the total number of source files loaded.
    /// </summary>
    public int SourceFileCount { get; }

    /// <summary>
    /// Gets the diagnostic messages from the loading process.
    /// </summary>
    public IReadOnlyList<DiagnosticMessage> Diagnostics { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadingResult"/> class.
    /// </summary>
    /// <param name="success">Whether loading succeeded.</param>
    /// <param name="usedFallback">Whether fallback was used.</param>
    /// <param name="fallbackReason">Reason for fallback.</param>
    /// <param name="loadTime">Total loading time.</param>
    /// <param name="projectCount">Number of projects loaded.</param>
    /// <param name="sourceFileCount">Number of source files loaded.</param>
    /// <param name="diagnostics">Diagnostic messages.</param>
    public LoadingResult(
        bool success,
        bool usedFallback = false,
        string? fallbackReason = null,
        TimeSpan loadTime = default,
        int projectCount = 0,
        int sourceFileCount = 0,
        IReadOnlyList<DiagnosticMessage>? diagnostics = null)
    {
        Success = success;
        UsedFallback = usedFallback;
        FallbackReason = fallbackReason;
        LoadTime = loadTime;
        ProjectCount = projectCount;
        SourceFileCount = sourceFileCount;
        Diagnostics = diagnostics ?? Array.Empty<DiagnosticMessage>();
    }

    /// <summary>
    /// Creates a successful loading result.
    /// </summary>
    /// <param name="loadTime">Loading time.</param>
    /// <param name="projectCount">Number of projects.</param>
    /// <param name="sourceFileCount">Number of source files.</param>
    /// <param name="diagnostics">Optional diagnostics.</param>
    /// <returns>A successful loading result.</returns>
    public static LoadingResult Successful(
        TimeSpan loadTime,
        int projectCount,
        int sourceFileCount,
        IReadOnlyList<DiagnosticMessage>? diagnostics = null)
    {
        return new LoadingResult(
            success: true,
            usedFallback: false,
            fallbackReason: null,
            loadTime: loadTime,
            projectCount: projectCount,
            sourceFileCount: sourceFileCount,
            diagnostics: diagnostics);
    }

    /// <summary>
    /// Creates a successful result that used fallback loading.
    /// </summary>
    /// <param name="fallbackReason">Why fallback was needed.</param>
    /// <param name="loadTime">Loading time.</param>
    /// <param name="projectCount">Number of projects.</param>
    /// <param name="sourceFileCount">Number of source files.</param>
    /// <param name="diagnostics">Optional diagnostics.</param>
    /// <returns>A successful loading result that used fallback.</returns>
    public static LoadingResult SuccessfulWithFallback(
        string fallbackReason,
        TimeSpan loadTime,
        int projectCount,
        int sourceFileCount,
        IReadOnlyList<DiagnosticMessage>? diagnostics = null)
    {
        return new LoadingResult(
            success: true,
            usedFallback: true,
            fallbackReason: fallbackReason,
            loadTime: loadTime,
            projectCount: projectCount,
            sourceFileCount: sourceFileCount,
            diagnostics: diagnostics);
    }

    /// <summary>
    /// Creates a failed loading result.
    /// </summary>
    /// <param name="diagnostics">Diagnostic messages explaining the failure.</param>
    /// <param name="loadTime">Time spent before failure.</param>
    /// <returns>A failed loading result.</returns>
    public static LoadingResult Failed(
        IReadOnlyList<DiagnosticMessage> diagnostics,
        TimeSpan loadTime = default)
    {
        return new LoadingResult(
            success: false,
            usedFallback: false,
            fallbackReason: null,
            loadTime: loadTime,
            projectCount: 0,
            sourceFileCount: 0,
            diagnostics: diagnostics);
    }
}
