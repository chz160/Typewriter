using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Typewriter.CLI.Commands;

namespace Typewriter.CLI;

/// <summary>
/// Entry point for the Typewriter CLI application.
/// </summary>
public static class Program
{
    /// <summary>
    /// CLI version string.
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// Main entry point.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Typewriter CLI - Generate TypeScript from C# using templates");

        // Add commands
        rootCommand.AddCommand(GenerateCommand.Create());
        rootCommand.AddCommand(InitCommand.Create());

        // Build command line parser with middleware
        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseExceptionHandler((ex, context) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();

                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"  {ex.InnerException.Message}");
                }

                context.ExitCode = ExitCodes.GenerationFailure;
            })
            .Build();

        return await parser.InvokeAsync(args);
    }
}

/// <summary>
/// Standard exit codes for the CLI.
/// </summary>
public static class ExitCodes
{
    /// <summary>
    /// All files generated successfully.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// Template errors, missing files, compilation errors.
    /// </summary>
    public const int GenerationFailure = 1;

    /// <summary>
    /// Bad CLI args, missing required options, config errors.
    /// </summary>
    public const int InvalidArguments = 2;
}
