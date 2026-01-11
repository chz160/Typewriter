using System;
using System.IO;
using Should;
using Xunit;
using Typewriter.CLI.Infrastructure;

namespace Typewriter.Tests.CLI
{
    [Trait("CLI", "ConsoleOutput")]
    public class ConsoleOutputTests
    {
        [Fact]
        public void Success_should_write_to_stdout_with_green_color()
        {
            var originalOut = Console.Out;
            try
            {
                using (var sw = new StringWriter())
                {
                    Console.SetOut(sw);
                    ConsoleOutput.Success("Test message");
                    var output = sw.ToString();
                    output.ShouldContain("Test message");
                    output.ShouldContain("\u001b[32m");
                    output.ShouldContain("\u001b[0m");
                }
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void Error_should_write_to_stderr_with_red_color()
        {
            var originalError = Console.Error;
            try
            {
                using (var sw = new StringWriter())
                {
                    Console.SetError(sw);
                    ConsoleOutput.Error("Error message");
                    var output = sw.ToString();
                    output.ShouldContain("Error message");
                    output.ShouldContain("\u001b[31m");
                    output.ShouldContain("\u001b[0m");
                }
            }
            finally
            {
                Console.SetError(originalError);
            }
        }

        [Fact]
        public void Warning_should_write_to_stdout_with_yellow_color()
        {
            var originalOut = Console.Out;
            try
            {
                using (var sw = new StringWriter())
                {
                    Console.SetOut(sw);
                    ConsoleOutput.Warning("Warning message");
                    var output = sw.ToString();
                    output.ShouldContain("Warning message");
                    output.ShouldContain("\u001b[33m");
                    output.ShouldContain("\u001b[0m");
                }
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void Info_should_write_to_stdout_without_color()
        {
            var originalOut = Console.Out;
            try
            {
                using (var sw = new StringWriter())
                {
                    Console.SetOut(sw);
                    ConsoleOutput.Info("Info message");
                    var output = sw.ToString();
                    output.ShouldContain("Info message");
                    output.ShouldNotContain("\u001b[");
                }
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void Error_should_use_stderr_not_stdout()
        {
            var originalOut = Console.Out;
            var originalError = Console.Error;
            try
            {
                using (var swOut = new StringWriter())
                using (var swError = new StringWriter())
                {
                    Console.SetOut(swOut);
                    Console.SetError(swError);
                    ConsoleOutput.Error("Error to stderr");

                    swOut.ToString().ShouldNotContain("Error to stderr");
                    swError.ToString().ShouldContain("Error to stderr");
                }
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }
    }
}
