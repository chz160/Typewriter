using System.CommandLine;
using System.Reflection;
using Typewriter.CLI.Commands;

namespace Typewriter.CLI
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var rootCommand = new RootCommand("Typewriter CLI - Generate TypeScript from C# templates");
            rootCommand.AddCommand(new GenerateCommand());
            return rootCommand.Invoke(args);
        }
    }
}
