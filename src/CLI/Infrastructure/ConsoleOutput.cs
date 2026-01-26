namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// Provides ANSI-colored console output utilities for CLI feedback.
/// </summary>
public class ConsoleOutput
{
    private bool _colorsEnabled = true;
    private readonly DiagnosticCollection _diagnostics = new();

    /// <summary>
    /// Gets the diagnostic collection for this output instance.
    /// </summary>
    public DiagnosticCollection Diagnostics => _diagnostics;

    /// <summary>
    /// Gets or sets whether ANSI color codes are enabled.
    /// </summary>
    public bool ColorsEnabled
    {
        get => _colorsEnabled && !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") == null;
        set => _colorsEnabled = value;
    }

    // ANSI color codes
    private const string Reset = "\x1b[0m";
    private const string Red = "\x1b[31m";
    private const string Green = "\x1b[32m";
    private const string Yellow = "\x1b[33m";
    private const string Cyan = "\x1b[36m";
    private const string Gray = "\x1b[90m";

    /// <summary>
    /// Writes a success message in green.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public void Success(string message)
    {
        WriteColored(message, Green, Console.Out);
    }

    /// <summary>
    /// Writes an error message in red to stderr.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public virtual void Error(string message)
    {
        _diagnostics.AddError(message);
        WriteColored($"Error: {message}", Red, Console.Error);
    }

    /// <summary>
    /// Writes an error message with location information to stderr.
    /// </summary>
    /// <param name="filePath">The file path where the error occurred.</param>
    /// <param name="line">The line number (optional).</param>
    /// <param name="column">The column number (optional).</param>
    /// <param name="message">The error message.</param>
    public void ErrorWithLocation(string filePath, int? line, int? column, string message)
    {
        var location = FormatLocation(filePath, line, column);
        WriteColored($"{location}: error: {message}", Red, Console.Error);
    }

    /// <summary>
    /// Writes a warning message in yellow to stderr.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public virtual void Warning(string message)
    {
        _diagnostics.AddWarning(message);
        WriteColored($"Warning: {message}", Yellow, Console.Error);
    }

    /// <summary>
    /// Writes a warning message with location information to stderr.
    /// </summary>
    /// <param name="filePath">The file path where the warning occurred.</param>
    /// <param name="line">The line number (optional).</param>
    /// <param name="column">The column number (optional).</param>
    /// <param name="message">The warning message.</param>
    public void WarningWithLocation(string filePath, int? line, int? column, string message)
    {
        var location = FormatLocation(filePath, line, column);
        WriteColored($"{location}: warning: {message}", Yellow, Console.Error);
    }

    /// <summary>
    /// Writes an informational message in default color.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public virtual void Info(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes a verbose/debug message in gray.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public virtual void Verbose(string message)
    {
        WriteColored(message, Gray, Console.Out);
    }

    /// <summary>
    /// Writes a header/section message in cyan.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public void Header(string message)
    {
        WriteColored(message, Cyan, Console.Out);
    }

    /// <summary>
    /// Writes the application banner with version information.
    /// </summary>
    /// <param name="version">The version string to display.</param>
    public void WriteBanner(string version)
    {
        Header($"Typewriter CLI v{version}");
    }

    /// <summary>
    /// Writes a generation summary.
    /// </summary>
    /// <param name="fileCount">Number of files generated.</param>
    /// <param name="duration">Time taken for generation.</param>
    /// <param name="warningCount">Number of warnings (optional).</param>
    public void WriteSummary(int fileCount, TimeSpan duration, int warningCount = 0)
    {
        var durationStr = duration.TotalSeconds >= 1
            ? $"{duration.TotalSeconds:F1}s"
            : $"{duration.TotalMilliseconds:F0}ms";

        var warningStr = warningCount > 0 ? $" ({warningCount} warning{(warningCount == 1 ? "" : "s")})" : "";

        Console.WriteLine();
        Success($"Generated {fileCount} TypeScript file{(fileCount == 1 ? "" : "s")}{warningStr} in {durationStr}");
    }

    /// <summary>
    /// Writes an error/warning count summary.
    /// </summary>
    /// <param name="errorCount">Number of errors.</param>
    /// <param name="warningCount">Number of warnings.</param>
    public void WriteErrorSummary(int errorCount, int warningCount)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine($"{errorCount} error{(errorCount == 1 ? "" : "s")}, {warningCount} warning{(warningCount == 1 ? "" : "s")}");
    }

    /// <summary>
    /// Writes a file generation result.
    /// </summary>
    /// <param name="relativePath">The relative path of the generated file.</param>
    /// <param name="bytes">The size in bytes (optional).</param>
    public void WriteFileGenerated(string relativePath, int? bytes = null)
    {
        var sizeStr = bytes.HasValue ? $" ({bytes} bytes)" : "";
        Console.WriteLine($"  - {relativePath}{sizeStr}");
    }

    /// <summary>
    /// Writes template processing progress.
    /// </summary>
    /// <param name="current">Current template number.</param>
    /// <param name="total">Total number of templates.</param>
    /// <param name="templateName">Name of the template being processed.</param>
    public void WriteProgress(int current, int total, string templateName)
    {
        Verbose($"[{current}/{total}] Processing {templateName}");
    }

    private void WriteColored(string message, string colorCode, TextWriter writer)
    {
        if (ColorsEnabled)
        {
            writer.WriteLine($"{colorCode}{message}{Reset}");
        }
        else
        {
            writer.WriteLine(message);
        }
    }

    private static string FormatLocation(string filePath, int? line, int? column)
    {
        if (line.HasValue && column.HasValue)
        {
            return $"{filePath}:{line}:{column}";
        }
        else if (line.HasValue)
        {
            return $"{filePath}:{line}";
        }
        return filePath;
    }
}
