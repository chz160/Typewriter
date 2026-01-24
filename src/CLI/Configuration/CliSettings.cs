using Typewriter.CodeModel;
using Typewriter.Configuration;
using Typewriter.VisualStudio;

namespace Typewriter.CLI.Configuration;

/// <summary>
/// CLI implementation of <see cref="Settings"/> that provides generation settings
/// without Visual Studio dependencies.
/// </summary>
public class CliSettings : Settings
{
    private readonly string _solutionFullName;
    private readonly string _templatePath;
    private readonly ILog _log;
    private bool _isSingleFileMode;
    private string? _singleFileName;
    private char _stringLiteralCharacter = '"';
    private bool _strictNullGeneration = true;
    private bool _utf8BomGeneration = true;
    private readonly HashSet<string> _includedProjects = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="CliSettings"/> class.
    /// </summary>
    /// <param name="solutionFullName">The full path to the solution.</param>
    /// <param name="templatePath">The full path to the template file.</param>
    /// <param name="log">The logger instance.</param>
    public CliSettings(string solutionFullName, string templatePath, ILog? log = null)
    {
        _solutionFullName = solutionFullName ?? throw new ArgumentNullException(nameof(solutionFullName));
        _templatePath = templatePath ?? throw new ArgumentNullException(nameof(templatePath));
        _log = log ?? new CliLog();
    }

    /// <inheritdoc />
    public override string SolutionFullName => _solutionFullName;

    /// <inheritdoc />
    public override bool IsSingleFileMode => _isSingleFileMode;

    /// <inheritdoc />
    public override string SingleFileName => _singleFileName ?? string.Empty;

    /// <inheritdoc />
    public override char StringLiteralCharacter => _stringLiteralCharacter;

    /// <inheritdoc />
    public override bool StrictNullGeneration => _strictNullGeneration;

    /// <inheritdoc />
    public override bool Utf8BomGeneration => _utf8BomGeneration;

    /// <inheritdoc />
    public override string TemplatePath => _templatePath;

    /// <inheritdoc />
    public override ILog Log => _log;

    /// <summary>
    /// Gets the set of included project names.
    /// </summary>
    public IReadOnlySet<string> IncludedProjects => _includedProjects;

    /// <inheritdoc />
    public override Settings IncludeProject(string projectName)
    {
        _includedProjects.Add(projectName);
        return this;
    }

    /// <inheritdoc />
    public override Settings SingleFileMode(string singleFilename)
    {
        _isSingleFileMode = true;
        _singleFileName = singleFilename;
        return this;
    }

    /// <inheritdoc />
    public override Settings IncludeCurrentProject()
    {
        // In CLI mode, we include all projects by default
        return this;
    }

    /// <inheritdoc />
    public override Settings IncludeReferencedProjects()
    {
        // In CLI mode, we include all projects by default
        return this;
    }

    /// <inheritdoc />
    public override Settings IncludeAllProjects()
    {
        // In CLI mode, we include all projects by default
        return this;
    }

    /// <inheritdoc />
    public override Settings UseStringLiteralCharacter(char ch)
    {
        _stringLiteralCharacter = ch;
        return this;
    }

    /// <inheritdoc />
    public override Settings DisableStrictNullGeneration()
    {
        _strictNullGeneration = false;
        return this;
    }

    /// <inheritdoc />
    public override Settings DisableUtf8BomGeneration()
    {
        _utf8BomGeneration = false;
        return this;
    }
}

/// <summary>
/// CLI implementation of <see cref="ILog"/> for console logging.
/// </summary>
public class CliLog : ILog
{
    /// <inheritdoc />
    public void LogDebug(string message, params object[] parameters)
    {
        // Only output in verbose mode
    }

    /// <inheritdoc />
    public void LogInfo(string message, params object[] parameters)
    {
        Console.WriteLine(string.Format(message, parameters));
    }

    /// <inheritdoc />
    public void LogWarning(string message, params object[] parameters)
    {
        Console.Error.WriteLine($"Warning: {string.Format(message, parameters)}");
    }

    /// <inheritdoc />
    public void LogError(string message, params object[] parameters)
    {
        Console.Error.WriteLine($"Error: {string.Format(message, parameters)}");
    }
}
