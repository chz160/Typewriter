using System.CommandLine;
using System.CommandLine.Parsing;
using Shouldly;
using Typewriter.CLI.Commands;
using Xunit;

namespace Typewriter.CLI.Tests;

public class GenerateCommandTests
{
    private readonly Command _command;

    public GenerateCommandTests()
    {
        _command = GenerateCommand.Create();
    }

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        _command.Name.ShouldBe("generate");
    }

    [Fact]
    public void Create_HasSolutionOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "solution");

        option.ShouldNotBeNull();
        option.Aliases.ShouldContain("-s");
        option.Aliases.ShouldContain("--solution");
    }

    [Fact]
    public void Create_HasProjectOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "project");

        option.ShouldNotBeNull();
        option.Aliases.ShouldContain("-p");
        option.Aliases.ShouldContain("--project");
    }

    [Fact]
    public void Create_HasConfigOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "config");

        option.ShouldNotBeNull();
        option.Aliases.ShouldContain("-c");
        option.Aliases.ShouldContain("--config");
    }

    [Fact]
    public void Create_HasVerboseOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "verbose");

        option.ShouldNotBeNull();
    }

    [Fact]
    public void Create_HasQuietOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "quiet");

        option.ShouldNotBeNull();
        option.Aliases.ShouldContain("-q");
        option.Aliases.ShouldContain("--quiet");
    }

    [Fact]
    public void Create_HasDryRunOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "dry-run");

        option.ShouldNotBeNull();
        option.Aliases.ShouldContain("-n");
        option.Aliases.ShouldContain("--dry-run");
    }

    [Fact]
    public void Create_HasJsonOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "json");

        option.ShouldNotBeNull();
    }

    [Fact]
    public void Parse_SolutionOption_ParsesCorrectly()
    {
        var result = _command.Parse("generate --solution ./Test.sln");

        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void Parse_SolutionOptionShort_ParsesCorrectly()
    {
        var result = _command.Parse("generate -s ./Test.sln");

        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void Parse_ProjectOption_ParsesCorrectly()
    {
        var result = _command.Parse("generate --project ./Test.csproj");

        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void Parse_ProjectOptionShort_ParsesCorrectly()
    {
        var result = _command.Parse("generate -p ./Test.csproj");

        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void Parse_VerboseFlag_ParsesCorrectly()
    {
        var result = _command.Parse("generate --verbose");

        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void Parse_QuietFlag_ParsesCorrectly()
    {
        var result = _command.Parse("generate --quiet");

        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void Parse_DryRunFlag_ParsesCorrectly()
    {
        var result = _command.Parse("generate --dry-run");

        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void Parse_JsonFlag_ParsesCorrectly()
    {
        var result = _command.Parse("generate --json");

        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void Parse_CombinedOptions_ParsesCorrectly()
    {
        var result = _command.Parse("generate -s ./Test.sln --verbose --dry-run");

        result.Errors.Count.ShouldBe(0);
    }

    // Note: --help is a built-in System.CommandLine feature
    // that gets added to the root command, not subcommands parsed in isolation

    [Fact]
    public void Parse_InvalidOption_ReturnsError()
    {
        var result = _command.Parse("generate --invalid-option");

        result.Errors.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Parse_SolutionWithSpaces_ParsesCorrectly()
    {
        var result = _command.Parse("generate --solution \"./My Project/Test.sln\"");

        result.Errors.Count.ShouldBe(0);
    }
}

public class ExitCodesTests
{
    [Fact]
    public void Success_IsZero()
    {
        ExitCodes.Success.ShouldBe(0);
    }

    [Fact]
    public void GenerationFailure_IsOne()
    {
        ExitCodes.GenerationFailure.ShouldBe(1);
    }

    [Fact]
    public void InvalidArguments_IsTwo()
    {
        ExitCodes.InvalidArguments.ShouldBe(2);
    }
}
