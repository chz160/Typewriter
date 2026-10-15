using Microsoft.CodeAnalysis;
using Shouldly;
using Typewriter.CLI.Infrastructure;
using Xunit;

namespace Typewriter.CLI.Tests;

[Trait("Category", "CLI")]
[Trait("Component", "CliRoslynWorkspace")]
public class CliRoslynWorkspaceTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "TypewriterWorkspaceTests", Guid.NewGuid().ToString("N"));
    private readonly CliRoslynWorkspace _workspace = new();

    public CliRoslynWorkspaceTests()
    {
        var domainDirectory = Path.Combine(_testDirectory, "Domain");
        var sharedDirectory = Path.Combine(_testDirectory, "Domain.Shared");
        Directory.CreateDirectory(domainDirectory);
        Directory.CreateDirectory(sharedDirectory);

        File.WriteAllText(Path.Combine(sharedDirectory, "Domain.Shared.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(sharedDirectory, "Shared.cs"), """
            namespace Domain.Shared
            {
                public interface IAmViewModel { }
                public sealed class GenerateTypeScriptAttribute : System.Attribute { }
            }
            """);
        File.WriteAllText(Path.Combine(domainDirectory, "Domain.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
              <ItemGroup><ProjectReference Include="../Domain.Shared/Domain.Shared.csproj" /></ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(domainDirectory, "ViewModel.cs"), """
            using Domain.Shared;
            namespace Domain
            {
                [GenerateTypeScript]
                public class ViewModel : IAmViewModel { }
            }
            """);
        File.WriteAllText(Path.Combine(_testDirectory, "Test.sln"), """
            Microsoft Visual Studio Solution File, Format Version 12.00
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Domain", "Domain/Domain.csproj", "{8D1EC147-9C25-4494-A8B7-8179E72E398B}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Domain.Shared", "Domain.Shared/Domain.Shared.csproj", "{BD904100-A87D-4178-B987-72EF973F88E2}"
            EndProject
            Global
            EndGlobal
            """);
    }

    [Fact]
    public async Task LoadSolutionAsync_ProjectReference_TargetsCopiedProject()
    {
        var domain = await LoadDomainAsync();
        var shared = _workspace.GetProjects().Single(p => p.Name == "Domain.Shared");

        domain.ProjectReferences.ShouldHaveSingleItem().ProjectId.ShouldBe(shared.Id);
        _workspace.Solution.GetProject(domain.ProjectReferences.Single().ProjectId).ShouldNotBeNull();
    }

    [Fact]
    public async Task LoadSolutionAsync_ProjectReference_ResolvesInterfaceAndAttribute()
    {
        var domain = await LoadDomainAsync();
        var compilation = await domain.GetCompilationAsync();
        compilation.ShouldNotBeNull();
        compilation.GetDiagnostics().Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ShouldBeEmpty();

        var viewModel = compilation.GetTypeByMetadataName("Domain.ViewModel");
        viewModel.ShouldNotBeNull();
        var interfaceSymbol = viewModel.Interfaces.ShouldHaveSingleItem();
        interfaceSymbol.TypeKind.ShouldBe(TypeKind.Interface);
        interfaceSymbol.ToDisplayString().ShouldBe("Domain.Shared.IAmViewModel");
        interfaceSymbol.ContainingAssembly.Name.ShouldBe("Domain.Shared");
        var attribute = viewModel.GetAttributes().ShouldHaveSingleItem().AttributeClass;
        attribute.ShouldNotBeNull();
        attribute.TypeKind.ShouldBe(TypeKind.Class);
        attribute.ToDisplayString().ShouldBe("Domain.Shared.GenerateTypeScriptAttribute");
        attribute.ContainingAssembly.Name.ShouldBe("Domain.Shared");
    }

    private async Task<Project> LoadDomainAsync()
    {
        var result = await _workspace.LoadSolutionWithResultAsync(Path.Combine(_testDirectory, "Test.sln"));
        result.Success.ShouldBeTrue();
        result.UsedFallback.ShouldBeFalse();
        _workspace.GetProjects().Count().ShouldBe(2);
        return _workspace.GetProjects().Single(p => p.Name == "Domain");
    }

    public void Dispose()
    {
        _workspace.Dispose();
        Directory.Delete(_testDirectory, recursive: true);
    }
}
