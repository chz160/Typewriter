namespace Typewriter.Core.Abstractions
{
    /// <summary>
    /// Defines the severity levels for diagnostic messages.
    /// </summary>
    public enum DiagnosticSeverity
    {
        /// <summary>
        /// Informational message (verbose mode only).
        /// </summary>
        Info,

        /// <summary>
        /// Non-blocking warning message.
        /// </summary>
        Warning,

        /// <summary>
        /// Blocking error message.
        /// </summary>
        Error
    }

    /// <summary>
    /// Provides methods for reporting errors, warnings, and informational messages
    /// during template generation.
    /// </summary>
    public interface IErrorReporter
    {
        /// <summary>
        /// Reports an error that occurred during generation.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="filePath">The file path where the error occurred (optional).</param>
        /// <param name="line">The line number where the error occurred (optional).</param>
        /// <param name="column">The column number where the error occurred (optional).</param>
        void ReportError(string message, string filePath = null, int? line = null, int? column = null);

        /// <summary>
        /// Reports a warning that occurred during generation.
        /// </summary>
        /// <param name="message">The warning message.</param>
        /// <param name="filePath">The file path where the warning occurred (optional).</param>
        /// <param name="line">The line number where the warning occurred (optional).</param>
        /// <param name="column">The column number where the warning occurred (optional).</param>
        void ReportWarning(string message, string filePath = null, int? line = null, int? column = null);

        /// <summary>
        /// Reports an informational message during generation.
        /// </summary>
        /// <param name="message">The informational message.</param>
        void ReportInfo(string message);

        /// <summary>
        /// Gets whether any errors have been reported.
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        /// Gets whether any warnings have been reported.
        /// </summary>
        bool HasWarnings { get; }

        /// <summary>
        /// Gets the total count of errors reported.
        /// </summary>
        int ErrorCount { get; }

        /// <summary>
        /// Gets the total count of warnings reported.
        /// </summary>
        int WarningCount { get; }
    }
}
