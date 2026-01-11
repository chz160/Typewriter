using System.CommandLine;
using System.CommandLine.Invocation;
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

            this.SetHandler(Execute, solutionOption);
        }

        private static void Execute(string solution)
        {
            if (string.IsNullOrEmpty(solution))
            {
                ConsoleOutput.Info("Generate command executed. Use --solution to specify a solution file.");
            }
            else
            {
                ConsoleOutput.Info(string.Concat("Generate command executed for solution: ", solution));
            }
        }
    }
}
