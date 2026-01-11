using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Typewriter.CLI.Infrastructure;

namespace Typewriter.CLI.Commands
{
    public class GenerateCommand : Command
    {
        public GenerateCommand()
            : base("generate", "Generate TypeScript files from C# code using templates")
        {
            var solutionOption = new Option<string>(
                aliases: new[] { "--solution", "-s" },
                description: "Path to the Visual Studio solution file (.sln)");

            AddOption(solutionOption);

            this.SetHandler((InvocationContext context) =>
            {
                var solution = context.ParseResult.GetValueForOption(solutionOption);
                context.ExitCode = Execute(solution);
            });
        }

        private static int Execute(string solution)
        {
            if (string.IsNullOrEmpty(solution))
            {
                ConsoleOutput.Error("Error: --solution argument is required");
                return 2;
            }

            if (!File.Exists(solution))
            {
                ConsoleOutput.Error(string.Concat(solution, ": Error: File not found"));
                return 2;
            }

            if (!solution.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                ConsoleOutput.Error(string.Concat(solution, ": Error: Not a solution file (.sln)"));
                return 2;
            }

            try
            {
                var provider = new CliMetadataProvider(solution);
                var workspace = provider.GetWorkspaceForValidation();
                ConsoleOutput.Success(string.Concat("Loaded solution: ", solution));
                return 0;
            }
            catch (InvalidOperationException ex)
            {
                ConsoleOutput.Error(string.Concat(solution, ": Error: ", ex.Message));
                return 1;
            }
            catch (FileNotFoundException ex)
            {
                ConsoleOutput.Error(string.Concat(solution, ": Error: ", ex.Message));
                return 1;
            }
            catch (Exception ex)
            {
                ConsoleOutput.Error(string.Concat(solution, ": Error: ", ex.Message));
                return 1;
            }
        }
    }
}
