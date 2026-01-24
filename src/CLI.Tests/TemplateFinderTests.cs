using Shouldly;
using Typewriter.CLI.Infrastructure;
using Xunit;

namespace Typewriter.CLI.Tests;

public sealed class TemplateFinderTests : IDisposable
{
    private readonly string _testDir;

    public TemplateFinderTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"TypewriterTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void FindTemplates_FindsTstFilesInRootDirectory()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDir, "Model.tst"), "template content");
        File.WriteAllText(Path.Combine(_testDir, "Api.tst"), "template content");

        var finder = new TemplateFinder(_testDir);

        // Act
        var templates = finder.FindTemplates();

        // Assert
        templates.Count.ShouldBe(2);
        templates.ShouldContain(t => t.EndsWith("Model.tst"));
        templates.ShouldContain(t => t.EndsWith("Api.tst"));
    }

    [Fact]
    public void FindTemplates_FindsTstFilesInSubdirectories()
    {
        // Arrange
        var subDir = Path.Combine(_testDir, "Templates");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "Model.tst"), "template content");

        var finder = new TemplateFinder(_testDir);

        // Act
        var templates = finder.FindTemplates();

        // Assert
        templates.Count.ShouldBe(1);
        templates[0].ShouldContain("Model.tst");
    }

    [Fact]
    public void FindTemplates_ExcludesObjDirectory()
    {
        // Arrange
        var objDir = Path.Combine(_testDir, "obj");
        Directory.CreateDirectory(objDir);
        File.WriteAllText(Path.Combine(_testDir, "Model.tst"), "template content");
        File.WriteAllText(Path.Combine(objDir, "Cached.tst"), "should be excluded");

        var finder = new TemplateFinder(_testDir);

        // Act
        var templates = finder.FindTemplates();

        // Assert
        templates.Count.ShouldBe(1);
        templates[0].ShouldContain("Model.tst");
        templates.ShouldNotContain(t => t.Contains("Cached.tst"));
    }

    [Fact]
    public void FindTemplates_ExcludesBinDirectory()
    {
        // Arrange
        var binDir = Path.Combine(_testDir, "bin");
        Directory.CreateDirectory(binDir);
        File.WriteAllText(Path.Combine(_testDir, "Model.tst"), "template content");
        File.WriteAllText(Path.Combine(binDir, "Output.tst"), "should be excluded");

        var finder = new TemplateFinder(_testDir);

        // Act
        var templates = finder.FindTemplates();

        // Assert
        templates.Count.ShouldBe(1);
        templates.ShouldNotContain(t => t.Contains("Output.tst"));
    }

    [Fact]
    public void FindTemplates_ExcludesNodeModulesDirectory()
    {
        // Arrange
        var nodeDir = Path.Combine(_testDir, "node_modules");
        Directory.CreateDirectory(nodeDir);
        File.WriteAllText(Path.Combine(_testDir, "Model.tst"), "template content");
        File.WriteAllText(Path.Combine(nodeDir, "package.tst"), "should be excluded");

        var finder = new TemplateFinder(_testDir);

        // Act
        var templates = finder.FindTemplates();

        // Assert
        templates.Count.ShouldBe(1);
        templates.ShouldNotContain(t => t.Contains("package.tst"));
    }

    [Fact]
    public void FindTemplates_ReturnsEmptyWhenNoTemplates()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDir, "readme.md"), "not a template");

        var finder = new TemplateFinder(_testDir);

        // Act
        var templates = finder.FindTemplates();

        // Assert
        templates.Count.ShouldBe(0);
    }

    [Fact]
    public void FindTemplates_ReturnsAbsolutePaths()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDir, "Model.tst"), "template content");

        var finder = new TemplateFinder(_testDir);

        // Act
        var templates = finder.FindTemplates();

        // Assert
        templates.Count.ShouldBe(1);
        Path.IsPathRooted(templates[0]).ShouldBeTrue();
    }

    [Fact]
    public void FindTemplates_SortsResultsAlphabetically()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDir, "Zebra.tst"), "content");
        File.WriteAllText(Path.Combine(_testDir, "Alpha.tst"), "content");
        File.WriteAllText(Path.Combine(_testDir, "Model.tst"), "content");

        var finder = new TemplateFinder(_testDir);

        // Act
        var templates = finder.FindTemplates();

        // Assert
        templates.Count.ShouldBe(3);
        Path.GetFileName(templates[0]).ShouldBe("Alpha.tst");
        Path.GetFileName(templates[1]).ShouldBe("Model.tst");
        Path.GetFileName(templates[2]).ShouldBe("Zebra.tst");
    }

    [Fact]
    public void FindInDirectory_FindsTemplatesInDirectory()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDir, "Model.tst"), "template content");

        // Act
        var templates = TemplateFinder.FindInDirectory(_testDir);

        // Assert
        templates.Count.ShouldBe(1);
    }

    [Fact]
    public void FindForSolutionOrProject_FindsTemplatesRelativeToFile()
    {
        // Arrange
        var slnPath = Path.Combine(_testDir, "Test.sln");
        File.WriteAllText(slnPath, "solution content");
        File.WriteAllText(Path.Combine(_testDir, "Model.tst"), "template content");

        // Act
        var templates = TemplateFinder.FindForSolutionOrProject(slnPath);

        // Assert
        templates.Count.ShouldBe(1);
    }

    [Fact]
    public void GetTemplateName_ReturnsFileNameWithoutExtension()
    {
        var name = TemplateFinder.GetTemplateName(@"C:\Projects\Templates\CustomerModel.tst");

        name.ShouldBe("CustomerModel");
    }

    [Fact]
    public void GetRelativePath_ReturnsRelativePath()
    {
        var templatePath = Path.Combine(_testDir, "Templates", "Model.tst");

        var relative = TemplateFinder.GetRelativePath(templatePath, _testDir);

        relative.ShouldBe(Path.Combine("Templates", "Model.tst"));
    }

    [Fact]
    public void Constructor_WithCustomIncludePatterns_UsesPatterns()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDir, "Model.tst"), "content");
        File.WriteAllText(Path.Combine(_testDir, "Model.template"), "content");

        var finder = new TemplateFinder(
            _testDir,
            includePatterns: new[] { "**/*.template" });

        // Act
        var templates = finder.FindTemplates();

        // Assert
        templates.Count.ShouldBe(1);
        templates[0].ShouldContain("Model.template");
    }

    [Fact]
    public void Constructor_WithCustomExcludePatterns_ExcludesPatterns()
    {
        // Arrange
        var customDir = Path.Combine(_testDir, "custom");
        Directory.CreateDirectory(customDir);
        File.WriteAllText(Path.Combine(_testDir, "Model.tst"), "content");
        File.WriteAllText(Path.Combine(customDir, "Excluded.tst"), "content");

        var finder = new TemplateFinder(
            _testDir,
            excludePatterns: new[] { "**/custom/**" });

        // Act
        var templates = finder.FindTemplates();

        // Assert
        templates.Count.ShouldBe(1);
        templates.ShouldNotContain(t => t.Contains("Excluded.tst"));
    }
}
