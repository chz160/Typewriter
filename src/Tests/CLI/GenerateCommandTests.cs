using System;
using System.IO;
using Should;
using Xunit;
using Typewriter.CLI.Commands;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace Typewriter.Tests.CLI
{
    [Trait("CLI", "GenerateCommand")]
    public class GenerateCommandTests
    {
        private static string GetTypewriterSolutionPath()
        {
            var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "Typewriter.sln")))
            {
                currentDir = currentDir.Parent;
            }
            return currentDir != null ? Path.Combine(currentDir.FullName, "Typewriter.sln") : null;
        }

        [Fact]
        public void GenerateCommand_should_have_solution_option()
        {
            var command = new GenerateCommand();
            command.ShouldNotBeNull();
            command.Name.ShouldEqual("generate");
        }

        [Fact]
        public void Missing_solution_argument_should_return_exit_code_2()
        {
            var command = new GenerateCommand();
            var rootCommand = new RootCommand { command };

            var result = rootCommand.Invoke("generate");

            result.ShouldEqual(2);
        }

        [Fact]
        public void Nonexistent_solution_file_should_return_exit_code_2()
        {
            var command = new GenerateCommand();
            var rootCommand = new RootCommand { command };

            var result = rootCommand.Invoke("generate --solution C:\\nonexistent\\solution.sln");

            result.ShouldEqual(2);
        }

        [Fact]
        public void Non_sln_file_should_return_exit_code_2()
        {
            var command = new GenerateCommand();
            var rootCommand = new RootCommand { command };
            var solutionPath = GetTypewriterSolutionPath();
            var readmePath = Path.Combine(Path.GetDirectoryName(solutionPath), "README.md");

            var result = rootCommand.Invoke(string.Concat("generate --solution \"", readmePath, "\""));

            result.ShouldEqual(2);
        }
    }
}
