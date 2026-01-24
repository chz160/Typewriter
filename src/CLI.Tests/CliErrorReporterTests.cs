using Shouldly;
using Typewriter.CLI.Infrastructure;
using Xunit;

namespace Typewriter.CLI.Tests;

public class CliErrorReporterTests
{
    [Fact]
    public void ReportError_AddsErrorToDiagnostics()
    {
        using var sw = new StringWriter();
        var originalErr = Console.Error;
        Console.SetError(sw);

        try
        {
            var output = new ConsoleOutput();
            var reporter = new CliErrorReporter(output);

            reporter.ReportError("Test error message");

            reporter.HasErrors.ShouldBeTrue();
            reporter.ErrorCount.ShouldBe(1);
            reporter.Diagnostics.Errors.First().Message.ShouldBe("Test error message");
        }
        finally
        {
            Console.SetError(originalErr);
        }
    }

    [Fact]
    public void ReportError_WithLocation_IncludesLocationInfo()
    {
        using var sw = new StringWriter();
        var originalErr = Console.Error;
        Console.SetError(sw);

        try
        {
            var output = new ConsoleOutput();
            var reporter = new CliErrorReporter(output);

            reporter.ReportError("Type not found", "template.tst", 15, 8);

            reporter.HasErrors.ShouldBeTrue();
            var error = reporter.Diagnostics.Errors.First();
            error.Message.ShouldBe("Type not found");
            error.FilePath.ShouldBe("template.tst");
            error.Line.ShouldBe(15);
            error.Column.ShouldBe(8);
        }
        finally
        {
            Console.SetError(originalErr);
        }
    }

    [Fact]
    public void ReportWarning_AddsWarningToDiagnostics()
    {
        using var sw = new StringWriter();
        var originalErr = Console.Error;
        Console.SetError(sw);

        try
        {
            var output = new ConsoleOutput();
            var reporter = new CliErrorReporter(output);

            reporter.ReportWarning("Test warning message");

            reporter.HasWarnings.ShouldBeTrue();
            reporter.HasErrors.ShouldBeFalse();
            reporter.WarningCount.ShouldBe(1);
            reporter.Diagnostics.Warnings.First().Message.ShouldBe("Test warning message");
        }
        finally
        {
            Console.SetError(originalErr);
        }
    }

    [Fact]
    public void ReportWarning_WithLocation_IncludesLocationInfo()
    {
        using var sw = new StringWriter();
        var originalErr = Console.Error;
        Console.SetError(sw);

        try
        {
            var output = new ConsoleOutput();
            var reporter = new CliErrorReporter(output);

            reporter.ReportWarning("Deprecated syntax", "old.tst", 5);

            var warning = reporter.Diagnostics.Warnings.First();
            warning.FilePath.ShouldBe("old.tst");
            warning.Line.ShouldBe(5);
        }
        finally
        {
            Console.SetError(originalErr);
        }
    }

    [Fact]
    public void Clear_RemovesAllDiagnostics()
    {
        using var sw = new StringWriter();
        var originalErr = Console.Error;
        Console.SetError(sw);

        try
        {
            var output = new ConsoleOutput();
            var reporter = new CliErrorReporter(output);

            reporter.ReportError("Error 1");
            reporter.ReportWarning("Warning 1");

            reporter.Clear();

            reporter.HasErrors.ShouldBeFalse();
            reporter.HasWarnings.ShouldBeFalse();
            reporter.ErrorCount.ShouldBe(0);
            reporter.WarningCount.ShouldBe(0);
        }
        finally
        {
            Console.SetError(originalErr);
        }
    }

    [Fact]
    public void MultipleErrors_TracksTotalCount()
    {
        using var sw = new StringWriter();
        var originalErr = Console.Error;
        Console.SetError(sw);

        try
        {
            var output = new ConsoleOutput();
            var reporter = new CliErrorReporter(output);

            reporter.ReportError("Error 1");
            reporter.ReportError("Error 2", "file.tst", 1);
            reporter.ReportWarning("Warning 1");
            reporter.ReportError("Error 3", "file.tst", 10, 5);

            reporter.ErrorCount.ShouldBe(3);
            reporter.WarningCount.ShouldBe(1);
        }
        finally
        {
            Console.SetError(originalErr);
        }
    }
}
