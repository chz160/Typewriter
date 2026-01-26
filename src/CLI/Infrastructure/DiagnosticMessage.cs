namespace Typewriter.CLI.Infrastructure;

/// <summary>
/// Defines the severity levels for diagnostic messages.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// Informational message.
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
/// Represents a diagnostic message (error or warning) from the generation process.
/// </summary>
public class DiagnosticMessage
{
    /// <summary>
    /// Gets or sets the severity of the diagnostic.
    /// </summary>
    public DiagnosticSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the human-readable message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path where the diagnostic occurred (optional).
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets the line number where the diagnostic occurred (optional).
    /// </summary>
    public int? Line { get; set; }

    /// <summary>
    /// Gets or sets the column number where the diagnostic occurred (optional).
    /// </summary>
    public int? Column { get; set; }

    /// <summary>
    /// Gets or sets the diagnostic code (e.g., TW001) (optional).
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Creates an error diagnostic message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="filePath">The file path (optional).</param>
    /// <param name="line">The line number (optional).</param>
    /// <param name="column">The column number (optional).</param>
    /// <returns>A new error diagnostic.</returns>
    public static DiagnosticMessage CreateError(string message, string? filePath = null, int? line = null, int? column = null)
    {
        return new DiagnosticMessage
        {
            Severity = DiagnosticSeverity.Error,
            Message = message,
            FilePath = filePath,
            Line = line,
            Column = column
        };
    }

    /// <summary>
    /// Creates a warning diagnostic message.
    /// </summary>
    /// <param name="message">The warning message.</param>
    /// <param name="filePath">The file path (optional).</param>
    /// <param name="line">The line number (optional).</param>
    /// <param name="column">The column number (optional).</param>
    /// <returns>A new warning diagnostic.</returns>
    public static DiagnosticMessage CreateWarning(string message, string? filePath = null, int? line = null, int? column = null)
    {
        return new DiagnosticMessage
        {
            Severity = DiagnosticSeverity.Warning,
            Message = message,
            FilePath = filePath,
            Line = line,
            Column = column
        };
    }

    /// <summary>
    /// Creates an info diagnostic message.
    /// </summary>
    /// <param name="message">The info message.</param>
    /// <returns>A new info diagnostic.</returns>
    public static DiagnosticMessage CreateInfo(string message)
    {
        return new DiagnosticMessage
        {
            Severity = DiagnosticSeverity.Info,
            Message = message
        };
    }

    /// <summary>
    /// Formats the diagnostic message in compiler-style format.
    /// </summary>
    /// <returns>The formatted message (e.g., "file.tst:15:8: error: message").</returns>
    public string FormatCompilerStyle()
    {
        var location = FormatLocation();
        var severity = Severity.ToString().ToLowerInvariant();

        if (!string.IsNullOrEmpty(location))
        {
            return $"{location}: {severity}: {Message}";
        }

        return $"{severity}: {Message}";
    }

    /// <summary>
    /// Formats just the location portion of the message.
    /// </summary>
    /// <returns>The formatted location (e.g., "file.tst:15:8").</returns>
    public string FormatLocation()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            return string.Empty;
        }

        if (Line.HasValue && Column.HasValue)
        {
            return $"{FilePath}:{Line}:{Column}";
        }

        if (Line.HasValue)
        {
            return $"{FilePath}:{Line}";
        }

        return FilePath;
    }

    /// <summary>
    /// Writes the diagnostic to the console using appropriate colors.
    /// </summary>
    /// <param name="output">The console output instance to use.</param>
    public void WriteToConsole(ConsoleOutput output)
    {
        switch (Severity)
        {
            case DiagnosticSeverity.Error:
                if (!string.IsNullOrEmpty(FilePath))
                {
                    output.ErrorWithLocation(FilePath, Line, Column, Message);
                }
                else
                {
                    output.Error(Message);
                }
                break;

            case DiagnosticSeverity.Warning:
                if (!string.IsNullOrEmpty(FilePath))
                {
                    output.WarningWithLocation(FilePath, Line, Column, Message);
                }
                else
                {
                    output.Warning(Message);
                }
                break;

            case DiagnosticSeverity.Info:
                output.Info(Message);
                break;
        }
    }

    /// <inheritdoc />
    public override string ToString() => FormatCompilerStyle();
}

/// <summary>
/// Collects and manages diagnostic messages during generation.
/// </summary>
public class DiagnosticCollection
{
    private readonly List<DiagnosticMessage> _diagnostics = new();

    /// <summary>
    /// Gets all diagnostics.
    /// </summary>
    public IReadOnlyList<DiagnosticMessage> All => _diagnostics;

    /// <summary>
    /// Gets all error diagnostics.
    /// </summary>
    public IEnumerable<DiagnosticMessage> Errors => _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Gets all warning diagnostics.
    /// </summary>
    public IEnumerable<DiagnosticMessage> Warnings => _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);

    /// <summary>
    /// Gets whether any errors have been reported.
    /// </summary>
    public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Gets whether any warnings have been reported.
    /// </summary>
    public bool HasWarnings => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning);

    /// <summary>
    /// Gets the count of errors.
    /// </summary>
    public int ErrorCount => _diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Gets the count of warnings.
    /// </summary>
    public int WarningCount => _diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

    /// <summary>
    /// Adds a diagnostic to the collection.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    public void Add(DiagnosticMessage diagnostic)
    {
        _diagnostics.Add(diagnostic);
    }

    /// <summary>
    /// Adds an error diagnostic to the collection.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="filePath">The file path (optional).</param>
    /// <param name="line">The line number (optional).</param>
    /// <param name="column">The column number (optional).</param>
    public void AddError(string message, string? filePath = null, int? line = null, int? column = null)
    {
        Add(DiagnosticMessage.CreateError(message, filePath, line, column));
    }

    /// <summary>
    /// Adds a warning diagnostic to the collection.
    /// </summary>
    /// <param name="message">The warning message.</param>
    /// <param name="filePath">The file path (optional).</param>
    /// <param name="line">The line number (optional).</param>
    /// <param name="column">The column number (optional).</param>
    public void AddWarning(string message, string? filePath = null, int? line = null, int? column = null)
    {
        Add(DiagnosticMessage.CreateWarning(message, filePath, line, column));
    }

    /// <summary>
    /// Writes all diagnostics to the console.
    /// </summary>
    /// <param name="output">The console output instance to use.</param>
    public void WriteAllToConsole(ConsoleOutput output)
    {
        foreach (var diagnostic in _diagnostics)
        {
            diagnostic.WriteToConsole(output);
        }
    }

    /// <summary>
    /// Writes the error/warning summary to the console.
    /// </summary>
    /// <param name="output">The console output instance to use.</param>
    public void WriteSummary(ConsoleOutput output)
    {
        if (HasErrors || HasWarnings)
        {
            output.WriteErrorSummary(ErrorCount, WarningCount);
        }
    }

    /// <summary>
    /// Clears all diagnostics.
    /// </summary>
    public void Clear()
    {
        _diagnostics.Clear();
    }

    /// <summary>
    /// Gets all diagnostic messages as a read-only list.
    /// </summary>
    /// <returns>A read-only list of all diagnostic messages.</returns>
    public IReadOnlyList<DiagnosticMessage> GetMessages()
    {
        return _diagnostics.ToList();
    }

    /// <summary>
    /// Converts diagnostic messages to the Models namespace format for LoadingResult.
    /// </summary>
    /// <returns>A list of diagnostic messages in the Models format.</returns>
    public IReadOnlyList<Models.DiagnosticMessage> ToModelDiagnostics()
    {
        return _diagnostics.Select(d => new Models.DiagnosticMessage(
            d.Severity switch
            {
                DiagnosticSeverity.Info => Models.DiagnosticSeverity.Info,
                DiagnosticSeverity.Warning => Models.DiagnosticSeverity.Warning,
                DiagnosticSeverity.Error => Models.DiagnosticSeverity.Error,
                _ => Models.DiagnosticSeverity.Info
            },
            d.Message,
            d.FilePath,
            d.Code
        )).ToList();
    }
}
