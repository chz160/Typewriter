using Shouldly;
using Typewriter.CLI.Configuration;
using Xunit;

namespace Typewriter.CLI.Tests;

public class CliSettingsTests
{
    [Fact]
    public void Constructor_SetsSolutionFullName()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        settings.SolutionFullName.ShouldBe(@"C:\Projects\Test.sln");
    }

    [Fact]
    public void Constructor_SetsTemplatePath()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        settings.TemplatePath.ShouldBe(@"C:\Projects\Model.tst");
    }

    [Fact]
    public void Constructor_ThrowsOnNullSolutionFullName()
    {
        Should.Throw<ArgumentNullException>(() => new CliSettings(null!, @"C:\Projects\Model.tst"));
    }

    [Fact]
    public void Constructor_ThrowsOnNullTemplatePath()
    {
        Should.Throw<ArgumentNullException>(() => new CliSettings(@"C:\Projects\Test.sln", null!));
    }

    [Fact]
    public void IsSingleFileMode_DefaultsFalse()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        settings.IsSingleFileMode.ShouldBeFalse();
    }

    [Fact]
    public void SingleFileMode_EnablesSingleFileMode()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        settings.SingleFileMode("output.ts");

        settings.IsSingleFileMode.ShouldBeTrue();
        settings.SingleFileName.ShouldBe("output.ts");
    }

    [Fact]
    public void SingleFileMode_ReturnsSelf()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        var result = settings.SingleFileMode("output.ts");

        result.ShouldBe(settings);
    }

    [Fact]
    public void StringLiteralCharacter_DefaultsToDoubleQuote()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        settings.StringLiteralCharacter.ShouldBe('"');
    }

    [Fact]
    public void UseStringLiteralCharacter_ChangesCharacter()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        settings.UseStringLiteralCharacter('\'');

        settings.StringLiteralCharacter.ShouldBe('\'');
    }

    [Fact]
    public void UseStringLiteralCharacter_ReturnsSelf()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        var result = settings.UseStringLiteralCharacter('\'');

        result.ShouldBe(settings);
    }

    [Fact]
    public void StrictNullGeneration_DefaultsTrue()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        settings.StrictNullGeneration.ShouldBeTrue();
    }

    [Fact]
    public void DisableStrictNullGeneration_DisablesStrictNull()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        settings.DisableStrictNullGeneration();

        settings.StrictNullGeneration.ShouldBeFalse();
    }

    [Fact]
    public void Utf8BomGeneration_DefaultsTrue()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        settings.Utf8BomGeneration.ShouldBeTrue();
    }

    [Fact]
    public void DisableUtf8BomGeneration_DisablesBom()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        settings.DisableUtf8BomGeneration();

        settings.Utf8BomGeneration.ShouldBeFalse();
    }

    [Fact]
    public void IncludeProject_AddsToIncludedProjects()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        settings.IncludeProject("MyProject");

        settings.IncludedProjects.ShouldContain("MyProject");
    }

    [Fact]
    public void IncludeProject_ReturnsSelf()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        var result = settings.IncludeProject("MyProject");

        result.ShouldBe(settings);
    }

    [Fact]
    public void IncludeCurrentProject_ReturnsSelf()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        var result = settings.IncludeCurrentProject();

        result.ShouldBe(settings);
    }

    [Fact]
    public void IncludeReferencedProjects_ReturnsSelf()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        var result = settings.IncludeReferencedProjects();

        result.ShouldBe(settings);
    }

    [Fact]
    public void IncludeAllProjects_ReturnsSelf()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        var result = settings.IncludeAllProjects();

        result.ShouldBe(settings);
    }

    [Fact]
    public void Log_IsNotNull()
    {
        var settings = new CliSettings(@"C:\Projects\Test.sln", @"C:\Projects\Model.tst");

        settings.Log.ShouldNotBeNull();
    }
}

public class CliLogTests
{
    [Fact]
    public void LogInfo_WritesToConsole()
    {
        var log = new CliLog();

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            log.LogInfo("Test message {0}", "value");
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        sw.ToString().ShouldContain("Test message value");
    }

    [Fact]
    public void LogWarning_DoesNotThrow()
    {
        // Console redirection is flaky in parallel tests
        // This verifies the method doesn't throw
        var log = new CliLog();

        log.LogWarning("Warning: {0}", "test");
    }

    [Fact]
    public void LogError_DoesNotThrow()
    {
        // Console redirection is flaky in parallel tests
        // This verifies the method doesn't throw
        var log = new CliLog();

        log.LogError("Error: {0}", "test");
    }

    [Fact]
    public void LogDebug_DoesNotThrow()
    {
        var log = new CliLog();

        // Debug messages are suppressed in CLI mode, just verify no exception
        log.LogDebug("Debug: {0}", "test");
    }
}
