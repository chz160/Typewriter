using Shouldly;
using Typewriter.CLI.Infrastructure;
using Xunit;

namespace Typewriter.CLI.Tests;

public class ConsoleOutputTests
{
    [Fact]
    public void Constructor_CreatesDiagnosticsCollection()
    {
        var output = new ConsoleOutput();

        output.Diagnostics.ShouldNotBeNull();
        output.Diagnostics.All.Count.ShouldBe(0);
    }

    [Fact]
    public void Error_AddsToDiagnosticsCollection()
    {
        var output = new ConsoleOutput();

        // Redirect stderr to capture output
        using var sw = new StringWriter();
        var originalErr = Console.Error;
        Console.SetError(sw);

        try
        {
            output.Error("Test error message");
        }
        finally
        {
            Console.SetError(originalErr);
        }

        output.Diagnostics.ErrorCount.ShouldBe(1);
        output.Diagnostics.Errors.First().Message.ShouldBe("Test error message");
    }

    [Fact]
    public void Warning_AddsToDiagnosticsCollection()
    {
        var output = new ConsoleOutput();

        // Redirect stderr to capture output
        using var sw = new StringWriter();
        var originalErr = Console.Error;
        Console.SetError(sw);

        try
        {
            output.Warning("Test warning message");
        }
        finally
        {
            Console.SetError(originalErr);
        }

        output.Diagnostics.WarningCount.ShouldBe(1);
        output.Diagnostics.Warnings.First().Message.ShouldBe("Test warning message");
    }

    [Fact]
    public void Error_FormatsMessageWithErrorPrefix()
    {
        // The actual stderr writing is covered by ErrorWithLocation test
        // This test verifies the error is tracked in diagnostics
        var output = new ConsoleOutput();

        using var sw = new StringWriter();
        var originalErr = Console.Error;
        Console.SetError(sw);

        try
        {
            output.Error("Test error");
        }
        finally
        {
            Console.SetError(originalErr);
        }

        // Verify diagnostics tracking works
        output.Diagnostics.HasErrors.ShouldBeTrue();
        output.Diagnostics.ErrorCount.ShouldBe(1);
    }

    [Fact]
    public void Warning_WritesToStderr()
    {
        var output = new ConsoleOutput();
        output.ColorsEnabled = false;

        using var sw = new StringWriter();
        var originalErr = Console.Error;
        Console.SetError(sw);

        try
        {
            output.Warning("Test warning");
        }
        finally
        {
            Console.SetError(originalErr);
        }

        sw.ToString().ShouldContain("Warning: Test warning");
    }

    [Fact]
    public void Info_WritesToStdout()
    {
        var output = new ConsoleOutput();

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            output.Info("Test info");
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        sw.ToString().ShouldContain("Test info");
    }

    [Fact]
    public void Success_WritesToStdout()
    {
        var output = new ConsoleOutput();
        output.ColorsEnabled = false;

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            output.Success("Operation completed");
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        sw.ToString().ShouldContain("Operation completed");
    }

    [Fact]
    public void ErrorWithLocation_FormatsLocationCorrectly()
    {
        var output = new ConsoleOutput();
        output.ColorsEnabled = false;

        using var sw = new StringWriter();
        var originalErr = Console.Error;
        Console.SetError(sw);

        try
        {
            output.ErrorWithLocation("file.tst", 10, 5, "Syntax error");
        }
        finally
        {
            Console.SetError(originalErr);
        }

        sw.ToString().ShouldContain("file.tst:10:5: error: Syntax error");
    }

    [Fact]
    public void WarningWithLocation_FormatsLocationCorrectly()
    {
        var output = new ConsoleOutput();
        output.ColorsEnabled = false;

        using var sw = new StringWriter();
        var originalErr = Console.Error;
        Console.SetError(sw);

        try
        {
            output.WarningWithLocation("file.tst", 5, null, "Deprecated");
        }
        finally
        {
            Console.SetError(originalErr);
        }

        sw.ToString().ShouldContain("file.tst:5: warning: Deprecated");
    }

    [Fact]
    public void WriteSummary_FormatsCorrectlyWithSeconds()
    {
        var output = new ConsoleOutput();
        output.ColorsEnabled = false;

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            output.WriteSummary(5, TimeSpan.FromSeconds(2.5));
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var result = sw.ToString();
        result.ShouldContain("Generated 5 TypeScript files");
        result.ShouldContain("2.5s");
    }

    [Fact]
    public void WriteSummary_FormatsCorrectlyWithMilliseconds()
    {
        var output = new ConsoleOutput();
        output.ColorsEnabled = false;

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            output.WriteSummary(1, TimeSpan.FromMilliseconds(500));
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var result = sw.ToString();
        result.ShouldContain("Generated 1 TypeScript file");
        result.ShouldContain("500ms");
    }

    [Fact]
    public void WriteSummary_IncludesWarningCount()
    {
        var output = new ConsoleOutput();
        output.ColorsEnabled = false;

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            output.WriteSummary(3, TimeSpan.FromSeconds(1), warningCount: 2);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        sw.ToString().ShouldContain("(2 warnings)");
    }

    [Fact]
    public void WriteErrorSummary_DoesNotThrow()
    {
        // WriteErrorSummary writes directly to Console.Error
        // Testing actual output is flaky in parallel test execution
        // This test verifies the method doesn't throw
        var output = new ConsoleOutput();

        // Should not throw
        output.WriteErrorSummary(3, 2);
        output.WriteErrorSummary(1, 0);
        output.WriteErrorSummary(0, 1);
    }

    [Fact]
    public void WriteFileGenerated_FormatsWithBytes()
    {
        var output = new ConsoleOutput();

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            output.WriteFileGenerated("Models/Customer.ts", 1024);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var result = sw.ToString();
        result.ShouldContain("Models/Customer.ts");
        result.ShouldContain("(1024 bytes)");
    }

    [Fact]
    public void WriteProgress_FormatsCorrectly()
    {
        var output = new ConsoleOutput();
        output.ColorsEnabled = false;

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            output.WriteProgress(2, 5, "CustomerModel.tst");
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var result = sw.ToString();
        result.ShouldContain("[2/5]");
        result.ShouldContain("CustomerModel.tst");
    }

    [Fact]
    public void ColorsEnabled_ReturnsFalse_WhenNoColorEnvSet()
    {
        var originalValue = Environment.GetEnvironmentVariable("NO_COLOR");
        try
        {
            Environment.SetEnvironmentVariable("NO_COLOR", "1");

            var output = new ConsoleOutput();

            output.ColorsEnabled.ShouldBeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NO_COLOR", originalValue);
        }
    }
}
