using System.Text.Json;
using Shouldly;
using Typewriter.CLI.Configuration;
using Xunit;

namespace Typewriter.CLI.Tests;

/// <summary>
/// Tests for config file generation logic (testing the pieces used by InitCommand).
/// </summary>
public class ConfigFileGenerationTests
{
    [Fact]
    public void ConfigFile_Serialization_ProducesValidJson()
    {
        var config = new ConfigFile
        {
            Solution = "TestSolution.sln",
            Templates = new List<string>(ConfigFile.DefaultTemplatePatterns),
            Exclude = new List<string>(ConfigFile.DefaultExcludePatterns),
            Verbosity = "normal"
        };

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(config, jsonOptions);
        var parsed = JsonDocument.Parse(json);

        parsed.RootElement.GetProperty("solution").GetString().ShouldBe("TestSolution.sln");
        parsed.RootElement.GetProperty("templates").GetArrayLength().ShouldBe(1);
        parsed.RootElement.GetProperty("verbosity").GetString().ShouldBe("normal");
    }

    [Fact]
    public void ConfigFile_Serialization_OmitsNullValues()
    {
        var config = new ConfigFile
        {
            Solution = "TestSolution.sln",
            Templates = new List<string>(ConfigFile.DefaultTemplatePatterns),
            Verbosity = "normal"
            // Project is null, should be omitted
        };

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(config, jsonOptions);

        json.ShouldNotContain("\"project\"");
    }

    [Fact]
    public void ConfigFile_Serialization_IncludesProject_WhenSolutionNull()
    {
        var config = new ConfigFile
        {
            Project = "TestProject.csproj",
            Templates = new List<string>(ConfigFile.DefaultTemplatePatterns),
            Verbosity = "normal"
        };

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(config, jsonOptions);
        var parsed = JsonDocument.Parse(json);

        parsed.RootElement.GetProperty("project").GetString().ShouldBe("TestProject.csproj");
    }

    [Fact]
    public void ConfigFile_DefaultPatterns_CorrectValues()
    {
        var config = ConfigFile.CreateDefault();

        config.Templates.ShouldNotBeNull();
        config.Templates.Count.ShouldBe(1);
        config.Templates.ShouldContain("**/*.tst");

        config.Exclude.ShouldNotBeNull();
        config.Exclude.Count.ShouldBe(3);
        config.Exclude.ShouldContain("**/obj/**");
        config.Exclude.ShouldContain("**/bin/**");
        config.Exclude.ShouldContain("**/node_modules/**");
    }

    [Fact]
    public void ConfigFile_WriteAndRead_RoundTrips()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var config = new ConfigFile
            {
                Solution = "MySolution.sln",
                Templates = new List<string> { "**/*.tst", "custom/**/*.tst" },
                Exclude = new List<string> { "**/generated/**" },
                Output = "dist",
                Verbosity = "verbose"
            };

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(config, jsonOptions);
            File.WriteAllText(tempFile, json);

            var loaded = ConfigFileLoader.LoadFromPath(tempFile);

            loaded.ShouldNotBeNull();
            loaded.Solution.ShouldBe("MySolution.sln");
            loaded.Templates.ShouldNotBeNull();
            loaded.Templates.Count.ShouldBe(2);
            loaded.Exclude.ShouldNotBeNull();
            loaded.Exclude.Count.ShouldBe(1);
            loaded.Output.ShouldBe("dist");
            loaded.Verbosity.ShouldBe("verbose");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ConfigFileLoader_ConfigFileNames_ContainsExpectedFormats()
    {
        ConfigFileLoader.ConfigFileNames.Length.ShouldBe(3);
        ConfigFileLoader.ConfigFileNames.ShouldContain(".typewriterrc");
        ConfigFileLoader.ConfigFileNames.ShouldContain("typewriter.json");
        ConfigFileLoader.ConfigFileNames.ShouldContain(".typewriterrc.json");
    }

    [Fact]
    public void ConfigFileLoader_ConfigFileNames_TypewriterrcFirst()
    {
        // .typewriterrc should have highest priority
        ConfigFileLoader.ConfigFileNames[0].ShouldBe(".typewriterrc");
    }
}
