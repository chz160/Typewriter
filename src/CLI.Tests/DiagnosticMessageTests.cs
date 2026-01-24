using Shouldly;
using Typewriter.CLI.Infrastructure;
using Xunit;

namespace Typewriter.CLI.Tests;

public class DiagnosticMessageTests
{
    [Fact]
    public void CreateError_SetsCorrectSeverity()
    {
        var diagnostic = DiagnosticMessage.CreateError("Test error");

        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
        diagnostic.Message.ShouldBe("Test error");
    }

    [Fact]
    public void CreateError_WithLocation_SetsAllProperties()
    {
        var diagnostic = DiagnosticMessage.CreateError(
            "Test error",
            filePath: "test.tst",
            line: 10,
            column: 5);

        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
        diagnostic.Message.ShouldBe("Test error");
        diagnostic.FilePath.ShouldBe("test.tst");
        diagnostic.Line.ShouldBe(10);
        diagnostic.Column.ShouldBe(5);
    }

    [Fact]
    public void CreateWarning_SetsCorrectSeverity()
    {
        var diagnostic = DiagnosticMessage.CreateWarning("Test warning");

        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
        diagnostic.Message.ShouldBe("Test warning");
    }

    [Fact]
    public void CreateInfo_SetsCorrectSeverity()
    {
        var diagnostic = DiagnosticMessage.CreateInfo("Test info");

        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Info);
        diagnostic.Message.ShouldBe("Test info");
    }

    [Fact]
    public void FormatCompilerStyle_WithFullLocation_FormatsCorrectly()
    {
        var diagnostic = DiagnosticMessage.CreateError(
            "Cannot resolve type",
            filePath: "CustomerModel.tst",
            line: 15,
            column: 8);

        var formatted = diagnostic.FormatCompilerStyle();

        formatted.ShouldBe("CustomerModel.tst:15:8: error: Cannot resolve type");
    }

    [Fact]
    public void FormatCompilerStyle_WithLineOnly_FormatsCorrectly()
    {
        var diagnostic = DiagnosticMessage.CreateWarning(
            "Deprecated type",
            filePath: "Model.tst",
            line: 5);

        var formatted = diagnostic.FormatCompilerStyle();

        formatted.ShouldBe("Model.tst:5: warning: Deprecated type");
    }

    [Fact]
    public void FormatCompilerStyle_WithFileOnly_FormatsCorrectly()
    {
        var diagnostic = DiagnosticMessage.CreateError(
            "File not found",
            filePath: "Missing.tst");

        var formatted = diagnostic.FormatCompilerStyle();

        formatted.ShouldBe("Missing.tst: error: File not found");
    }

    [Fact]
    public void FormatCompilerStyle_WithNoLocation_FormatsCorrectly()
    {
        var diagnostic = DiagnosticMessage.CreateError("General error");

        var formatted = diagnostic.FormatCompilerStyle();

        formatted.ShouldBe("error: General error");
    }

    [Fact]
    public void FormatLocation_WithFullLocation_FormatsCorrectly()
    {
        var diagnostic = DiagnosticMessage.CreateError(
            "Error",
            filePath: "file.tst",
            line: 10,
            column: 5);

        var location = diagnostic.FormatLocation();

        location.ShouldBe("file.tst:10:5");
    }

    [Fact]
    public void FormatLocation_WithNoFile_ReturnsEmpty()
    {
        var diagnostic = DiagnosticMessage.CreateError("Error");

        var location = diagnostic.FormatLocation();

        location.ShouldBe(string.Empty);
    }

    [Fact]
    public void ToString_ReturnsCompilerStyleFormat()
    {
        var diagnostic = DiagnosticMessage.CreateError(
            "Test",
            filePath: "file.tst",
            line: 1,
            column: 1);

        var result = diagnostic.ToString();

        result.ShouldBe("file.tst:1:1: error: Test");
    }
}

public class DiagnosticCollectionTests
{
    [Fact]
    public void Add_AddsDiagnosticToCollection()
    {
        var collection = new DiagnosticCollection();
        var diagnostic = DiagnosticMessage.CreateError("Test");

        collection.Add(diagnostic);

        collection.All.Count.ShouldBe(1);
        collection.All[0].ShouldBe(diagnostic);
    }

    [Fact]
    public void AddError_CreatesAndAddsDiagnostic()
    {
        var collection = new DiagnosticCollection();

        collection.AddError("Error message", "file.tst", 10, 5);

        collection.All.Count.ShouldBe(1);
        collection.All[0].Severity.ShouldBe(DiagnosticSeverity.Error);
        collection.All[0].Message.ShouldBe("Error message");
        collection.All[0].FilePath.ShouldBe("file.tst");
    }

    [Fact]
    public void AddWarning_CreatesAndAddsDiagnostic()
    {
        var collection = new DiagnosticCollection();

        collection.AddWarning("Warning message");

        collection.All.Count.ShouldBe(1);
        collection.All[0].Severity.ShouldBe(DiagnosticSeverity.Warning);
    }

    [Fact]
    public void HasErrors_ReturnsTrueWhenErrorsExist()
    {
        var collection = new DiagnosticCollection();
        collection.AddError("Error");

        collection.HasErrors.ShouldBeTrue();
        collection.HasWarnings.ShouldBeFalse();
    }

    [Fact]
    public void HasWarnings_ReturnsTrueWhenWarningsExist()
    {
        var collection = new DiagnosticCollection();
        collection.AddWarning("Warning");

        collection.HasWarnings.ShouldBeTrue();
        collection.HasErrors.ShouldBeFalse();
    }

    [Fact]
    public void ErrorCount_ReturnsCorrectCount()
    {
        var collection = new DiagnosticCollection();
        collection.AddError("Error 1");
        collection.AddError("Error 2");
        collection.AddWarning("Warning 1");

        collection.ErrorCount.ShouldBe(2);
        collection.WarningCount.ShouldBe(1);
    }

    [Fact]
    public void Errors_ReturnsOnlyErrors()
    {
        var collection = new DiagnosticCollection();
        collection.AddError("Error");
        collection.AddWarning("Warning");

        var errors = collection.Errors.ToList();

        errors.Count.ShouldBe(1);
        errors[0].Message.ShouldBe("Error");
    }

    [Fact]
    public void Warnings_ReturnsOnlyWarnings()
    {
        var collection = new DiagnosticCollection();
        collection.AddError("Error");
        collection.AddWarning("Warning");

        var warnings = collection.Warnings.ToList();

        warnings.Count.ShouldBe(1);
        warnings[0].Message.ShouldBe("Warning");
    }

    [Fact]
    public void Clear_RemovesAllDiagnostics()
    {
        var collection = new DiagnosticCollection();
        collection.AddError("Error");
        collection.AddWarning("Warning");

        collection.Clear();

        collection.All.Count.ShouldBe(0);
        collection.HasErrors.ShouldBeFalse();
        collection.HasWarnings.ShouldBeFalse();
    }
}
