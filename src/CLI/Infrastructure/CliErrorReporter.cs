using Typewriter.Core.Abstractions;

namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// CLI implementation of IErrorReporter that outputs errors to console
/// and tracks diagnostic counts.
/// </summary>
public class CliErrorReporter : IErrorReporter
{
    private readonly ConsoleOutput _output;
    private readonly DiagnosticCollection _diagnostics;

    public CliErrorReporter(ConsoleOutput output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _diagnostics = new DiagnosticCollection();
    }

    /// <inheritdoc />
    public void ReportError(string message, string? filePath = null, int? line = null, int? column = null)
    {
        _diagnostics.AddError(message, filePath, line, column);

        if (filePath != null)
        {
            _output.ErrorWithLocation(filePath, line, column, message);
        }
        else
        {
            _output.Error(message);
        }
    }

    /// <inheritdoc />
    public void ReportWarning(string message, string? filePath = null, int? line = null, int? column = null)
    {
        _diagnostics.AddWarning(message, filePath, line, column);

        if (filePath != null)
        {
            _output.WarningWithLocation(filePath, line, column, message);
        }
        else
        {
            _output.Warning(message);
        }
    }

    /// <inheritdoc />
    public void ReportInfo(string message)
    {
        _output.Info(message);
    }

    /// <inheritdoc />
    public bool HasErrors => _diagnostics.HasErrors;

    /// <inheritdoc />
    public bool HasWarnings => _diagnostics.HasWarnings;

    /// <inheritdoc />
    public int ErrorCount => _diagnostics.ErrorCount;

    /// <inheritdoc />
    public int WarningCount => _diagnostics.WarningCount;

    /// <summary>
    /// Gets the underlying diagnostic collection.
    /// </summary>
    public DiagnosticCollection Diagnostics => _diagnostics;

    /// <summary>
    /// Clears all reported diagnostics.
    /// </summary>
    public void Clear()
    {
        _diagnostics.Clear();
    }
}
