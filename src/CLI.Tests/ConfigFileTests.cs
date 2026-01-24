using System.Text.Json;
using Shouldly;
using Typewriter.CLI.Configuration;
using Xunit;

namespace Typewriter.CLI.Tests;

public class ConfigFileTests
{
    [Fact]
    public void ConfigFile_HasDefaultPatterns()
    {
        ConfigFile.DefaultTemplatePatterns.ShouldContain("**/*.tst");
        ConfigFile.DefaultExcludePatterns.ShouldContain("**/obj/**");
        ConfigFile.DefaultExcludePatterns.ShouldContain("**/bin/**");
        ConfigFile.DefaultExcludePatterns.ShouldContain("**/node_modules/**");
    }

    [Fact]
    public void ConfigFile_CreateDefault_SetsDefaultValues()
    {
        var config = ConfigFile.CreateDefault();

        config.Templates.ShouldNotBeNull();
        config.Templates.ShouldContain("**/*.tst");
        config.Exclude.ShouldNotBeNull();
        config.Exclude.ShouldContain("**/obj/**");
        config.Verbosity.ShouldBe("normal");
    }

    [Fact]
    public void ConfigFile_ResolvePath_ReturnsAbsolutePathUnchanged()
    {
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(Path.GetTempPath(), "test", ".typewriterrc")
        };

        var absolutePath = Path.Combine(Path.GetTempPath(), "absolute", "path.sln");
        var result = config.ResolvePath(absolutePath);

        result.ShouldBe(absolutePath);
    }

    [Fact]
    public void ConfigFile_ResolvePath_ResolvesRelativePathFromConfigDir()
    {
        var configDir = Path.Combine(Path.GetTempPath(), "configtest");
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(configDir, ".typewriterrc")
        };

        var result = config.ResolvePath("MySolution.sln");

        result.ShouldBe(Path.GetFullPath(Path.Combine(configDir, "MySolution.sln")));
    }

    [Fact]
    public void ConfigFile_GetResolvedSolutionPath_ReturnsNullWhenSolutionNotSet()
    {
        var config = new ConfigFile();

        config.GetResolvedSolutionPath().ShouldBeNull();
    }

    [Fact]
    public void ConfigFile_GetResolvedSolutionPath_ResolvesPath()
    {
        var configDir = Path.Combine(Path.GetTempPath(), "configtest");
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(configDir, ".typewriterrc"),
            Solution = "MySolution.sln"
        };

        var result = config.GetResolvedSolutionPath();

        result.ShouldBe(Path.GetFullPath(Path.Combine(configDir, "MySolution.sln")));
    }

    [Fact]
    public void ConfigFile_GetResolvedProjectPath_ReturnsNullWhenProjectNotSet()
    {
        var config = new ConfigFile();

        config.GetResolvedProjectPath().ShouldBeNull();
    }

    [Fact]
    public void ConfigFile_GetResolvedProjectPath_ResolvesPath()
    {
        var configDir = Path.Combine(Path.GetTempPath(), "configtest");
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(configDir, ".typewriterrc"),
            Project = "MyProject.csproj"
        };

        var result = config.GetResolvedProjectPath();

        result.ShouldBe(Path.GetFullPath(Path.Combine(configDir, "MyProject.csproj")));
    }

    [Fact]
    public void ConfigFile_GetResolvedOutputPath_ReturnsNullWhenOutputNotSet()
    {
        var config = new ConfigFile();

        config.GetResolvedOutputPath().ShouldBeNull();
    }

    [Fact]
    public void ConfigFile_GetResolvedOutputPath_ResolvesPath()
    {
        var configDir = Path.Combine(Path.GetTempPath(), "configtest");
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(configDir, ".typewriterrc"),
            Output = "generated"
        };

        var result = config.GetResolvedOutputPath();

        result.ShouldBe(Path.GetFullPath(Path.Combine(configDir, "generated")));
    }

    [Theory]
    [InlineData("quiet", true, false)]
    [InlineData("QUIET", true, false)]
    [InlineData("verbose", false, true)]
    [InlineData("VERBOSE", false, true)]
    [InlineData("normal", false, false)]
    [InlineData("invalid", false, false)]
    [InlineData(null, false, false)]
    public void ConfigFile_ParseVerbosity_ReturnsCorrectFlags(string? verbosity, bool expectedQuiet, bool expectedVerbose)
    {
        var config = new ConfigFile { Verbosity = verbosity };

        var (quiet, verbose) = config.ParseVerbosity();

        quiet.ShouldBe(expectedQuiet);
        verbose.ShouldBe(expectedVerbose);
    }

    [Fact]
    public void ConfigFile_ConfigDirectory_ReturnsDirectoryOfConfigPath()
    {
        var configDir = Path.Combine(Path.GetTempPath(), "configtest");
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(configDir, ".typewriterrc")
        };

        config.ConfigDirectory.ShouldBe(configDir);
    }

    [Fact]
    public void ConfigFile_ConfigDirectory_ReturnsNullWhenConfigPathNotSet()
    {
        var config = new ConfigFile();

        config.ConfigDirectory.ShouldBeNull();
    }
}

public sealed class ConfigFileLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public ConfigFileLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"typewriter_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void ConfigFileNames_ContainsExpectedNames()
    {
        ConfigFileLoader.ConfigFileNames.ShouldContain(".typewriterrc");
        ConfigFileLoader.ConfigFileNames.ShouldContain("typewriter.json");
        ConfigFileLoader.ConfigFileNames.ShouldContain(".typewriterrc.json");
    }

    [Fact]
    public void LoadFromPath_ReturnsNullForNonExistentFile()
    {
        var result = ConfigFileLoader.LoadFromPath(Path.Combine(_tempDir, "nonexistent.json"));

        result.ShouldBeNull();
    }

    [Fact]
    public void LoadFromPath_LoadsValidConfigFile()
    {
        var configPath = Path.Combine(_tempDir, ".typewriterrc");
        File.WriteAllText(configPath, @"{
            ""solution"": ""MySolution.sln"",
            ""templates"": [""**/*.tst"", ""custom/**/*.tst""],
            ""verbosity"": ""verbose""
        }");

        var result = ConfigFileLoader.LoadFromPath(configPath);

        result.ShouldNotBeNull();
        result.Solution.ShouldBe("MySolution.sln");
        result.Templates.ShouldNotBeNull();
        result.Templates.Count.ShouldBe(2);
        result.Templates.ShouldContain("**/*.tst");
        result.Verbosity.ShouldBe("verbose");
        result.ConfigPath.ShouldBe(Path.GetFullPath(configPath));
    }

    [Fact]
    public void LoadFromPath_HandlesCommentsAndTrailingCommas()
    {
        var configPath = Path.Combine(_tempDir, "typewriter.json");
        File.WriteAllText(configPath, @"{
            // This is a comment
            ""solution"": ""MySolution.sln"",
            ""templates"": [""**/*.tst"",], // trailing comma
        }");

        var result = ConfigFileLoader.LoadFromPath(configPath);

        result.ShouldNotBeNull();
        result.Solution.ShouldBe("MySolution.sln");
    }

    [Fact]
    public void LoadFromPath_ReturnsNullForInvalidJson()
    {
        var configPath = Path.Combine(_tempDir, ".typewriterrc");
        File.WriteAllText(configPath, "not valid json {{{");

        var result = ConfigFileLoader.LoadFromPath(configPath);

        result.ShouldBeNull();
    }

    [Fact]
    public void LoadFromPath_IsCaseInsensitive()
    {
        var configPath = Path.Combine(_tempDir, ".typewriterrc");
        File.WriteAllText(configPath, @"{
            ""Solution"": ""MySolution.sln"",
            ""VERBOSITY"": ""quiet""
        }");

        var result = ConfigFileLoader.LoadFromPath(configPath);

        result.ShouldNotBeNull();
        result.Solution.ShouldBe("MySolution.sln");
        result.Verbosity.ShouldBe("quiet");
    }

    [Fact]
    public void Discover_ReturnsNullWhenNoConfigFound()
    {
        var emptyDir = Path.Combine(_tempDir, "empty");
        Directory.CreateDirectory(emptyDir);

        var result = ConfigFileLoader.Discover(emptyDir);

        result.ShouldBeNull();
    }

    [Fact]
    public void Discover_FindsTypewriterrc()
    {
        var configPath = Path.Combine(_tempDir, ".typewriterrc");
        File.WriteAllText(configPath, @"{ ""solution"": ""Test.sln"" }");

        var result = ConfigFileLoader.Discover(_tempDir);

        result.ShouldNotBeNull();
        result.Solution.ShouldBe("Test.sln");
    }

    [Fact]
    public void Discover_FindsTypewriterJson()
    {
        var configPath = Path.Combine(_tempDir, "typewriter.json");
        File.WriteAllText(configPath, @"{ ""project"": ""Test.csproj"" }");

        var result = ConfigFileLoader.Discover(_tempDir);

        result.ShouldNotBeNull();
        result.Project.ShouldBe("Test.csproj");
    }

    [Fact]
    public void Discover_PrefersTypewriterrcOverTypewriterJson()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".typewriterrc"), @"{ ""solution"": ""FromRc.sln"" }");
        File.WriteAllText(Path.Combine(_tempDir, "typewriter.json"), @"{ ""solution"": ""FromJson.sln"" }");

        var result = ConfigFileLoader.Discover(_tempDir);

        result.ShouldNotBeNull();
        result.Solution.ShouldBe("FromRc.sln");
    }

    [Fact]
    public void Discover_SearchesSolutionDirectory()
    {
        var projectDir = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(projectDir);

        // Config in solution root
        File.WriteAllText(Path.Combine(_tempDir, ".typewriterrc"), @"{ ""solution"": ""Root.sln"" }");

        var result = ConfigFileLoader.Discover(projectDir, Path.Combine(_tempDir, "Test.sln"));

        result.ShouldNotBeNull();
        result.Solution.ShouldBe("Root.sln");
    }

    [Fact]
    public void Discover_PrefersCurrentDirectoryOverSolutionDirectory()
    {
        var projectDir = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(projectDir);

        File.WriteAllText(Path.Combine(_tempDir, ".typewriterrc"), @"{ ""solution"": ""Root.sln"" }");
        File.WriteAllText(Path.Combine(projectDir, ".typewriterrc"), @"{ ""solution"": ""Local.sln"" }");

        var result = ConfigFileLoader.Discover(projectDir, Path.Combine(_tempDir, "Test.sln"));

        result.ShouldNotBeNull();
        result.Solution.ShouldBe("Local.sln");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}

public class MergedSettingsTests
{
    [Fact]
    public void Merge_UsesCliSolutionOverConfig()
    {
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(Path.GetTempPath(), ".typewriterrc"),
            Solution = "Config.sln"
        };

        var result = ConfigFileLoader.Merge(config, "CLI.sln", null, false, false);

        result.SolutionPath.ShouldBe("CLI.sln");
        result.ProjectPath.ShouldBeNull();
    }

    [Fact]
    public void Merge_UsesCliProjectOverConfig()
    {
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(Path.GetTempPath(), ".typewriterrc"),
            Project = "Config.csproj"
        };

        var result = ConfigFileLoader.Merge(config, null, "CLI.csproj", false, false);

        result.ProjectPath.ShouldBe("CLI.csproj");
        result.SolutionPath.ShouldBeNull();
    }

    [Fact]
    public void Merge_UsesConfigSolutionWhenCliNotProvided()
    {
        var configDir = Path.GetTempPath();
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(configDir, ".typewriterrc"),
            Solution = "Config.sln"
        };

        var result = ConfigFileLoader.Merge(config, null, null, false, false);

        result.SolutionPath.ShouldBe(Path.GetFullPath(Path.Combine(configDir, "Config.sln")));
    }

    [Fact]
    public void Merge_UsesConfigProjectWhenCliNotProvided()
    {
        var configDir = Path.GetTempPath();
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(configDir, ".typewriterrc"),
            Project = "Config.csproj"
        };

        var result = ConfigFileLoader.Merge(config, null, null, false, false);

        result.ProjectPath.ShouldBe(Path.GetFullPath(Path.Combine(configDir, "Config.csproj")));
    }

    [Fact]
    public void Merge_CliVerboseTakesPrecedence()
    {
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(Path.GetTempPath(), ".typewriterrc"),
            Verbosity = "quiet"
        };

        var result = ConfigFileLoader.Merge(config, null, null, cliVerbose: true, cliQuiet: false);

        result.Verbose.ShouldBeTrue();
        result.Quiet.ShouldBeFalse();
    }

    [Fact]
    public void Merge_CliQuietTakesPrecedence()
    {
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(Path.GetTempPath(), ".typewriterrc"),
            Verbosity = "verbose"
        };

        var result = ConfigFileLoader.Merge(config, null, null, cliVerbose: false, cliQuiet: true);

        result.Quiet.ShouldBeTrue();
        result.Verbose.ShouldBeFalse();
    }

    [Fact]
    public void Merge_UsesConfigVerbosityWhenCliNotSet()
    {
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(Path.GetTempPath(), ".typewriterrc"),
            Verbosity = "verbose"
        };

        var result = ConfigFileLoader.Merge(config, null, null, false, false);

        result.Verbose.ShouldBeTrue();
        result.Quiet.ShouldBeFalse();
    }

    [Fact]
    public void Merge_UsesConfigTemplatePatterns()
    {
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(Path.GetTempPath(), ".typewriterrc"),
            Templates = new List<string> { "custom/**/*.tst" }
        };

        var result = ConfigFileLoader.Merge(config, null, null, false, false);

        result.TemplatePatterns.ShouldContain("custom/**/*.tst");
        result.TemplatePatterns.Count.ShouldBe(1);
    }

    [Fact]
    public void Merge_UsesDefaultTemplatePatternsWhenConfigNull()
    {
        var result = ConfigFileLoader.Merge(null, null, null, false, false);

        result.TemplatePatterns.ShouldContain("**/*.tst");
    }

    [Fact]
    public void Merge_UsesConfigExcludePatterns()
    {
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(Path.GetTempPath(), ".typewriterrc"),
            Exclude = new List<string> { "**/generated/**" }
        };

        var result = ConfigFileLoader.Merge(config, null, null, false, false);

        result.ExcludePatterns.ShouldContain("**/generated/**");
        result.ExcludePatterns.Count.ShouldBe(1);
    }

    [Fact]
    public void Merge_UsesDefaultExcludePatternsWhenConfigNull()
    {
        var result = ConfigFileLoader.Merge(null, null, null, false, false);

        result.ExcludePatterns.ShouldContain("**/obj/**");
        result.ExcludePatterns.ShouldContain("**/bin/**");
    }

    [Fact]
    public void Merge_SetsOutputDirectoryFromConfig()
    {
        var configDir = Path.GetTempPath();
        var config = new ConfigFile
        {
            ConfigPath = Path.Combine(configDir, ".typewriterrc"),
            Output = "generated"
        };

        var result = ConfigFileLoader.Merge(config, null, null, false, false);

        result.OutputDirectory.ShouldBe(Path.GetFullPath(Path.Combine(configDir, "generated")));
    }

    [Fact]
    public void Merge_SetsConfigPath()
    {
        var configPath = Path.Combine(Path.GetTempPath(), ".typewriterrc");
        var config = new ConfigFile
        {
            ConfigPath = configPath
        };

        var result = ConfigFileLoader.Merge(config, null, null, false, false);

        result.ConfigPath.ShouldBe(configPath);
        result.HasConfig.ShouldBeTrue();
    }

    [Fact]
    public void Merge_HasConfigFalseWhenNoConfig()
    {
        var result = ConfigFileLoader.Merge(null, null, null, false, false);

        result.HasConfig.ShouldBeFalse();
        result.ConfigPath.ShouldBeNull();
    }

    [Fact]
    public void MergedSettings_TargetPath_ReturnsSolutionFirst()
    {
        var settings = new MergedSettings
        {
            SolutionPath = "MySolution.sln",
            ProjectPath = "MyProject.csproj"
        };

        settings.TargetPath.ShouldBe("MySolution.sln");
        settings.IsSolution.ShouldBeTrue();
    }

    [Fact]
    public void MergedSettings_TargetPath_ReturnsProjectWhenNoSolution()
    {
        var settings = new MergedSettings
        {
            ProjectPath = "MyProject.csproj"
        };

        settings.TargetPath.ShouldBe("MyProject.csproj");
        settings.IsSolution.ShouldBeFalse();
    }

    [Fact]
    public void MergedSettings_TargetPath_ReturnsNullWhenNeither()
    {
        var settings = new MergedSettings();

        settings.TargetPath.ShouldBeNull();
        settings.IsSolution.ShouldBeFalse();
    }
}
