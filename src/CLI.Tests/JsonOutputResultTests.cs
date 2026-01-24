using System.Text.Json;
using Shouldly;
using Typewriter.CLI.Generation;
using Typewriter.CLI.Infrastructure;
using Typewriter.CLI.Output;
using Xunit;

namespace Typewriter.CLI.Tests;

public class JsonOutputResultTests
{
    [Fact]
    public void Create_WithNoErrors_HasSuccessTrue()
    {
        var files = new List<GeneratedFile>();
        var result = JsonOutputResult.Create(
            "1.0.0",
            "/path/to/solution.sln",
            3,
            files,
            TimeSpan.FromSeconds(1.5),
            false,
            null);

        result.Success.ShouldBeTrue();
        result.ErrorCount.ShouldBe(0);
        result.WarningCount.ShouldBe(0);
    }

    [Fact]
    public void Create_WithErrors_HasSuccessFalse()
    {
        using var sw = new StringWriter();
        Console.SetError(sw);

        try
        {
            var output = new ConsoleOutput();
            var errorReporter = new CliErrorReporter(output);
            errorReporter.ReportError("Test error");

            var files = new List<GeneratedFile>();
            var result = JsonOutputResult.Create(
                "1.0.0",
                "/path/to/solution.sln",
                3,
                files,
                TimeSpan.FromSeconds(1.5),
                false,
                errorReporter);

            result.Success.ShouldBeFalse();
            result.ErrorCount.ShouldBe(1);
        }
        finally
        {
            Console.SetError(Console.Error);
        }
    }

    [Fact]
    public void Create_IncludesGeneratedFiles()
    {
        var files = new List<GeneratedFile>
        {
            new GeneratedFile("Models/Customer.ts", "/templates/Model.tst", 1024),
            new GeneratedFile("Models/Order.ts", "/templates/Model.tst", 512)
        };

        var result = JsonOutputResult.Create(
            "1.0.0",
            "/path/to/solution.sln",
            1,
            files,
            TimeSpan.FromSeconds(0.5),
            false,
            null);

        result.FilesGenerated.ShouldBe(2);
        result.Files.Count.ShouldBe(2);
        result.Files[0].Path.ShouldBe("Models/Customer.ts");
        result.Files[0].Bytes.ShouldBe(1024);
        result.Files[1].Path.ShouldBe("Models/Order.ts");
    }

    [Fact]
    public void Create_IncludesDiagnostics()
    {
        using var sw = new StringWriter();
        Console.SetError(sw);

        try
        {
            var output = new ConsoleOutput();
            var errorReporter = new CliErrorReporter(output);
            errorReporter.ReportError("Type not found", "template.tst", 15, 8);
            errorReporter.ReportWarning("Deprecated syntax", "old.tst", 5);

            var files = new List<GeneratedFile>();
            var result = JsonOutputResult.Create(
                "1.0.0",
                "/path/to/solution.sln",
                2,
                files,
                TimeSpan.FromSeconds(1),
                false,
                errorReporter);

            result.Errors.Count.ShouldBe(1);
            result.Errors[0].Message.ShouldBe("Type not found");
            result.Errors[0].File.ShouldBe("template.tst");
            result.Errors[0].Line.ShouldBe(15);
            result.Errors[0].Column.ShouldBe(8);

            result.Warnings.Count.ShouldBe(1);
            result.Warnings[0].Message.ShouldBe("Deprecated syntax");
        }
        finally
        {
            Console.SetError(Console.Error);
        }
    }

    [Fact]
    public void Create_DryRun_SetsFlag()
    {
        var files = new List<GeneratedFile>();
        var result = JsonOutputResult.Create(
            "1.0.0",
            "/path/to/solution.sln",
            1,
            files,
            TimeSpan.FromSeconds(0.1),
            dryRun: true,
            null);

        result.DryRun.ShouldBeTrue();
    }

    [Fact]
    public void Create_FormatsShortDuration_AsMilliseconds()
    {
        var files = new List<GeneratedFile>();
        var result = JsonOutputResult.Create(
            "1.0.0",
            "/path/to/solution.sln",
            1,
            files,
            TimeSpan.FromMilliseconds(500),
            false,
            null);

        result.Duration.ShouldBe("500ms");
        result.DurationMs.ShouldBe(500);
    }

    [Fact]
    public void Create_FormatsLongDuration_AsSeconds()
    {
        var files = new List<GeneratedFile>();
        var result = JsonOutputResult.Create(
            "1.0.0",
            "/path/to/solution.sln",
            1,
            files,
            TimeSpan.FromSeconds(2.5),
            false,
            null);

        result.Duration.ShouldBe("2.5s");
    }

    [Fact]
    public void ToJson_ProducesValidJson()
    {
        var files = new List<GeneratedFile>
        {
            new GeneratedFile("Models/Customer.ts", "/templates/Model.tst", 1024)
        };

        var result = JsonOutputResult.Create(
            "1.0.0",
            "/path/to/solution.sln",
            1,
            files,
            TimeSpan.FromSeconds(1),
            false,
            null);

        var json = result.ToJson();

        // Should be valid JSON
        var parsed = JsonDocument.Parse(json);
        parsed.RootElement.GetProperty("success").GetBoolean().ShouldBeTrue();
        parsed.RootElement.GetProperty("version").GetString().ShouldBe("1.0.0");
        parsed.RootElement.GetProperty("filesGenerated").GetInt32().ShouldBe(1);
        parsed.RootElement.GetProperty("files").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public void ToJson_OmitsNullValues()
    {
        var files = new List<GeneratedFile>();
        var result = JsonOutputResult.Create(
            "1.0.0",
            "/path/to/solution.sln",
            1,
            files,
            TimeSpan.FromSeconds(1),
            false,
            null);

        var json = result.ToJson();

        // Errors and warnings arrays should be empty, not omitted
        var parsed = JsonDocument.Parse(json);
        parsed.RootElement.GetProperty("errors").GetArrayLength().ShouldBe(0);
        parsed.RootElement.GetProperty("warnings").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public void ToJson_WithIndentedFalse_ProducesCompactJson()
    {
        var files = new List<GeneratedFile>();
        var result = JsonOutputResult.Create(
            "1.0.0",
            "/path/to/solution.sln",
            1,
            files,
            TimeSpan.FromSeconds(1),
            false,
            null);

        var json = result.ToJson(indented: false);

        // Should not contain newlines (compact format)
        json.ShouldNotContain("\n");
    }
}

public class JsonFileInfoTests
{
    [Fact]
    public void JsonFileInfo_StoresAllProperties()
    {
        var fileInfo = new JsonFileInfo
        {
            Path = "Models/Customer.ts",
            Template = "Model.tst",
            Bytes = 2048
        };

        fileInfo.Path.ShouldBe("Models/Customer.ts");
        fileInfo.Template.ShouldBe("Model.tst");
        fileInfo.Bytes.ShouldBe(2048);
    }
}

public class JsonDiagnosticTests
{
    [Fact]
    public void JsonDiagnostic_StoresAllProperties()
    {
        var diagnostic = new JsonDiagnostic
        {
            Message = "Type not found",
            File = "template.tst",
            Line = 15,
            Column = 8
        };

        diagnostic.Message.ShouldBe("Type not found");
        diagnostic.File.ShouldBe("template.tst");
        diagnostic.Line.ShouldBe(15);
        diagnostic.Column.ShouldBe(8);
    }

    [Fact]
    public void JsonDiagnostic_AllowsNullLocation()
    {
        var diagnostic = new JsonDiagnostic
        {
            Message = "General error"
        };

        diagnostic.File.ShouldBeNull();
        diagnostic.Line.ShouldBeNull();
        diagnostic.Column.ShouldBeNull();
    }
}
