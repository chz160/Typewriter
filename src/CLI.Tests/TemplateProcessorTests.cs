using Shouldly;
using Typewriter.CLI.Generation;
using Xunit;

namespace Typewriter.CLI.Tests;

public class TemplateSyntaxErrorTests
{
    [Fact]
    public void TemplateSyntaxError_StoresAllProperties()
    {
        var error = new TemplateSyntaxError(
            "Cannot resolve type",
            15,
            8,
            true);

        error.Message.ShouldBe("Cannot resolve type");
        error.Line.ShouldBe(15);
        error.Column.ShouldBe(8);
        error.IsError.ShouldBeTrue();
    }

    [Fact]
    public void TemplateSyntaxError_Warning_HasIsErrorFalse()
    {
        var warning = new TemplateSyntaxError(
            "Deprecated syntax",
            5,
            null,
            false);

        warning.IsError.ShouldBeFalse();
    }
}

public class TemplateSettingsTests
{
    [Fact]
    public void TemplateSettings_HasDefaultValues()
    {
        var settings = new TemplateSettings();

        settings.OutputExtension.ShouldBe(".ts");
        settings.FilterPattern.ShouldBeNull();
    }

    [Fact]
    public void TemplateSettings_CanSetAllProperties()
    {
        var settings = new TemplateSettings
        {
            TemplatePath = "/path/to/template.tst",
            FilterPattern = "*Model",
            OutputDirectory = "/output",
            OutputExtension = ".d.ts",
            IncludeProjectPattern = "MyProject"
        };

        settings.TemplatePath.ShouldBe("/path/to/template.tst");
        settings.FilterPattern.ShouldBe("*Model");
        settings.OutputDirectory.ShouldBe("/output");
        settings.OutputExtension.ShouldBe(".d.ts");
        settings.IncludeProjectPattern.ShouldBe("MyProject");
    }
}

public class TemplateResultTests
{
    [Fact]
    public void TemplateResult_InitializesWithTemplatePath()
    {
        var result = new TemplateResult("/path/to/template.tst");

        result.TemplatePath.ShouldBe("/path/to/template.tst");
        result.Success.ShouldBeFalse();
        result.GeneratedFiles.Count.ShouldBe(0);
    }

    [Fact]
    public void TemplateResult_CanAddGeneratedFiles()
    {
        var result = new TemplateResult("/path/to/template.tst");

        result.AddGeneratedFile(new GeneratedFile("Models/Customer.ts", "/path/to/template.tst", 1024));
        result.AddGeneratedFile(new GeneratedFile("Models/Order.ts", "/path/to/template.tst", 512));

        result.GeneratedFiles.Count.ShouldBe(2);
        result.GeneratedFiles[0].RelativePath.ShouldBe("Models/Customer.ts");
        result.GeneratedFiles[1].ByteCount.ShouldBe(512);
    }
}

public class GeneratedFileTests
{
    [Fact]
    public void GeneratedFile_StoresAllProperties()
    {
        var file = new GeneratedFile(
            "Models/Customer.ts",
            "/templates/Model.tst",
            2048);

        file.RelativePath.ShouldBe("Models/Customer.ts");
        file.TemplatePath.ShouldBe("/templates/Model.tst");
        file.ByteCount.ShouldBe(2048);
    }
}

public class TemplateRenderExceptionTests
{
    [Fact]
    public void TemplateRenderException_StoresMessage()
    {
        var ex = new TemplateRenderException("Cannot resolve type 'Customer'");

        ex.Message.ShouldBe("Cannot resolve type 'Customer'");
        ex.FilePath.ShouldBeNull();
        ex.Line.ShouldBeNull();
        ex.Column.ShouldBeNull();
    }

    [Fact]
    public void TemplateRenderException_WithLocation_StoresAllProperties()
    {
        var ex = new TemplateRenderException(
            "Undefined identifier",
            "template.tst",
            15,
            8);

        ex.Message.ShouldBe("Undefined identifier");
        ex.FilePath.ShouldBe("template.tst");
        ex.Line.ShouldBe(15);
        ex.Column.ShouldBe(8);
    }
}
