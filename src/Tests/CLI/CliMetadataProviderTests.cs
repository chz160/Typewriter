using System;
using System.IO;
using Should;
using Xunit;
using Typewriter.CLI.Infrastructure;

namespace Typewriter.Tests.CLI
{
    [Trait("CLI", "CliMetadataProvider")]
    public class CliMetadataProviderTests
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
        public void Constructor_with_valid_solution_should_not_throw()
        {
            var solutionPath = GetTypewriterSolutionPath();
            solutionPath.ShouldNotBeNull();

            var provider = new CliMetadataProvider(solutionPath);
            provider.ShouldNotBeNull();
        }

        [Fact]
        public void Constructor_with_null_path_should_throw_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CliMetadataProvider(null));
        }

    }
}
