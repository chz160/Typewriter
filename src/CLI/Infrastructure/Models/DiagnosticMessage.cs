namespace Typewriter.CLI.Infrastructure.Models;

/// <summary>
/// Severity level for diagnostic messages.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// Informational message.
    /// </summary>
    Info,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warning,

    /// <summary>
    /// Error message.
    /// </summary>
    Error
}

/// <summary>
/// A diagnostic message from the loading process.
/// </summary>
public sealed class DiagnosticMessage
{
    /// <summary>
    /// Gets the severity of the diagnostic.
    /// </summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>
    /// Gets the human-readable message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the related file path, if applicable.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Gets the diagnostic code for programmatic handling.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticMessage"/> class.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <param name="message">The message text.</param>
    /// <param name="filePath">Optional related file path.</param>
    /// <param name="code">Optional diagnostic code.</param>
    public DiagnosticMessage(DiagnosticSeverity severity, string message, string? filePath = null, string? code = null)
    {
        Severity = severity;
        Message = message;
        FilePath = filePath;
        Code = code;
    }

    /// <summary>
    /// Creates an info diagnostic message.
    /// </summary>
    /// <param name="message">The message text.</param>
    /// <param name="filePath">Optional related file path.</param>
    /// <returns>A new diagnostic message.</returns>
    public static DiagnosticMessage Info(string message, string? filePath = null)
        => new(DiagnosticSeverity.Info, message, filePath);

    /// <summary>
    /// Creates a warning diagnostic message.
    /// </summary>
    /// <param name="message">The message text.</param>
    /// <param name="filePath">Optional related file path.</param>
    /// <returns>A new diagnostic message.</returns>
    public static DiagnosticMessage Warning(string message, string? filePath = null)
        => new(DiagnosticSeverity.Warning, message, filePath);

    /// <summary>
    /// Creates an error diagnostic message.
    /// </summary>
    /// <param name="message">The message text.</param>
    /// <param name="filePath">Optional related file path.</param>
    /// <returns>A new diagnostic message.</returns>
    public static DiagnosticMessage Error(string message, string? filePath = null)
        => new(DiagnosticSeverity.Error, message, filePath);
}
