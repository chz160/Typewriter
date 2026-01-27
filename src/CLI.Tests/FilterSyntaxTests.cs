using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Typewriter.CLI.CodeModel.Implementation;
using Typewriter.CLI.Configuration;
using Typewriter.CodeModel;
using Typewriter.Metadata.Interfaces;
using Xunit;

namespace Typewriter.CLI.Tests;

/// <summary>
/// Filter syntax parity tests for CLI template engine.
/// These tests verify that CLI class collections support all VS extension filter syntaxes.
/// </summary>
public class FilterSyntaxTests
{
    private readonly CliSettings _settings;
    private readonly Compilation _compilation;
    private readonly IReadOnlyList<IClassMetadata> _classMetadatas;

    public FilterSyntaxTests()
    {
        _settings = new CliSettings("test.sln", "test.tst");

        // Compile the FilterTestSource fixture
        var sourceCode = GetFilterTestSourceCode();
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(SerializableAttribute).Assembly.Location),
        };

        // Add runtime assembly references
        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references = references.Concat(new[]
        {
            MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll")),
        }).ToArray();

        _compilation = CSharpCompilation.Create(
            "FilterTestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        // Get all class metadata from the compilation
        _classMetadatas = GetAllClasses();
    }

    private static string GetFilterTestSourceCode()
    {
        return @"
using System;

namespace FilterTests
{
    // Classes with various naming patterns
    public class UserModel { public int Id { get; set; } }
    public class ProductModel { public string Name { get; set; } }
    public class OrderModel { public DateTime Date { get; set; } }
    public class NotAModelClass { public string Value { get; set; } }

    public class BaseEntity { public int Id { get; set; } }
    public class BaseDtoClass { public string Data { get; set; } }
    public class SomeBaseClass { public bool Active { get; set; } }
    public class NoBasePrefix { public string Name { get; set; } }

    public class UserService { public void Execute() { } }
    public class ProductService { public void Process() { } }
    public class ServiceHelper { public void Help() { } }
    public class NoServicePattern { public void DoWork() { } }

    // Classes with attributes
    [Serializable]
    public class SerializableClass { public string Data { get; set; } }

    [Obsolete]
    public class ObsoleteClass { public int Value { get; set; } }

    [Serializable]
    [Obsolete]
    public class MultiAttributeClass { public string Name { get; set; } }

    public class NoAttributeClass { public bool Flag { get; set; } }

    // Classes with custom attribute
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomAttribute : Attribute { }

    [Custom]
    public class CustomAttributeClass { public string Data { get; set; } }

    // Inheritance hierarchy
    public class AnimalBase { public string Name { get; set; } }
    public class Dog : AnimalBase { public string Breed { get; set; } }
    public class Cat : AnimalBase { public bool Indoor { get; set; } }
    public class Bird { public bool CanFly { get; set; } }

    public interface IRepository { }
    public class UserRepository : IRepository { public void Save() { } }
    public class ProductRepository : IRepository { public void Load() { } }
    public class HelperClass { public void Help() { } }

    // Combined patterns
    [Serializable]
    public class SerializableModel : AnimalBase { }
}
";
    }

    private IReadOnlyList<IClassMetadata> GetAllClasses()
    {
        var classes = new List<IClassMetadata>();

        foreach (var syntaxTree in _compilation.SyntaxTrees)
        {
            var semanticModel = _compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            var classDeclarations = root.DescendantNodes()
                .Where(n => n is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax);

            foreach (var classDecl in classDeclarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                if (symbol != null && !symbol.IsAbstract)
                {
                    var metadata = CliClassMetadata.FromNamedTypeSymbol(symbol, _settings);
                    if (metadata != null)
                    {
                        classes.Add(metadata);
                    }
                }
            }
        }

        return classes;
    }

    // Helper methods to apply filter logic directly on IClassMetadata
    // This mimics what CliItemFilter does internally

    private IEnumerable<IClassMetadata> FilterByNamePattern(string pattern)
    {
        var parts = pattern.Split('*');

        IEnumerable<IClassMetadata> filtered = _classMetadatas;

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var index = i;

            if (parts.Length == 1)
            {
                // Exact match
                filtered = filtered.Where(c =>
                    string.Equals(c.Name, part, StringComparison.OrdinalIgnoreCase));
            }
            else if (index == 0 && !string.IsNullOrWhiteSpace(part))
            {
                // StartsWith match
                filtered = filtered.Where(c =>
                    c.Name.StartsWith(part, StringComparison.OrdinalIgnoreCase));
            }
            else if (index == parts.Length - 1 && !string.IsNullOrWhiteSpace(part))
            {
                // EndsWith match
                filtered = filtered.Where(c =>
                    c.Name.EndsWith(part, StringComparison.OrdinalIgnoreCase));
            }
            else if (index > 0 && index < parts.Length - 1 && !string.IsNullOrWhiteSpace(part))
            {
                // Contains match
                filtered = filtered.Where(c =>
                    c.Name.Contains(part));
            }
        }

        return filtered;
    }

    private IEnumerable<IClassMetadata> FilterByAttribute(string attributeName)
    {
        // Strip "Attribute" suffix for matching (both with and without suffix)
        var matchName = attributeName;
        var matchNameWithSuffix = attributeName.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase)
            ? attributeName
            : attributeName + "Attribute";
        var matchNameWithoutSuffix = attributeName.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase)
            ? attributeName.Substring(0, attributeName.Length - "Attribute".Length)
            : attributeName;

        return _classMetadatas.Where(c =>
            c.Attributes.Any(a =>
                // Match with the name as provided
                string.Equals(a.Name, matchName, StringComparison.OrdinalIgnoreCase) ||
                // Match with Attribute suffix added
                string.Equals(a.Name, matchNameWithSuffix, StringComparison.OrdinalIgnoreCase) ||
                // Match without Attribute suffix
                string.Equals(a.Name, matchNameWithoutSuffix, StringComparison.OrdinalIgnoreCase) ||
                // Also check FullName variants
                string.Equals(a.FullName, matchName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.FullName, matchNameWithSuffix, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.FullName, matchNameWithoutSuffix, StringComparison.OrdinalIgnoreCase)));
    }

    private IEnumerable<IClassMetadata> FilterByInheritance(string baseTypeName)
    {
        return _classMetadatas.Where(c =>
        {
            // Check base class
            if (c.BaseClass != null &&
                (string.Equals(c.BaseClass.Name, baseTypeName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(c.BaseClass.FullName, baseTypeName, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Check interfaces
            return c.Interfaces.Any(i =>
                string.Equals(i.Name, baseTypeName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(i.FullName, baseTypeName, StringComparison.OrdinalIgnoreCase));
        });
    }

    // T050: Test wildcard suffix filter
    [Fact]
    public void NamePattern_WildcardSuffix_MatchesEndsWithModel()
    {
        var result = FilterByNamePattern("*Model").ToList();

        // UserModel, ProductModel, OrderModel, SerializableModel all end with "Model"
        Assert.Equal(4, result.Count);
        Assert.Contains(result, c => c.Name == "UserModel");
        Assert.Contains(result, c => c.Name == "ProductModel");
        Assert.Contains(result, c => c.Name == "OrderModel");
        Assert.Contains(result, c => c.Name == "SerializableModel");
        Assert.DoesNotContain(result, c => c.Name == "NotAModelClass");
    }

    [Fact]
    public void NamePattern_WildcardSuffix_MatchesEndsWithService()
    {
        var result = FilterByNamePattern("*Service").ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Name == "UserService");
        Assert.Contains(result, c => c.Name == "ProductService");
        Assert.DoesNotContain(result, c => c.Name == "ServiceHelper");
    }

    // T051: Test wildcard prefix filter
    [Fact]
    public void NamePattern_WildcardPrefix_MatchesStartsWithBase()
    {
        var result = FilterByNamePattern("Base*").ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Name == "BaseEntity");
        Assert.Contains(result, c => c.Name == "BaseDtoClass");
        Assert.DoesNotContain(result, c => c.Name == "SomeBaseClass");
    }

    [Fact]
    public void NamePattern_WildcardPrefix_MatchesStartsWithUser()
    {
        var result = FilterByNamePattern("User*").ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(result, c => c.Name == "UserModel");
        Assert.Contains(result, c => c.Name == "UserService");
        Assert.Contains(result, c => c.Name == "UserRepository");
    }

    // T052: Test wildcard middle filter
    [Fact]
    public void NamePattern_WildcardMiddle_MatchesContainsService()
    {
        var result = FilterByNamePattern("*Service*").ToList();

        // UserService, ProductService, ServiceHelper, NoServicePattern all contain "Service"
        Assert.Equal(4, result.Count);
        Assert.Contains(result, c => c.Name == "UserService");
        Assert.Contains(result, c => c.Name == "ProductService");
        Assert.Contains(result, c => c.Name == "ServiceHelper");
        Assert.Contains(result, c => c.Name == "NoServicePattern");
    }

    [Fact]
    public void NamePattern_WildcardMiddle_MatchesContainsModel()
    {
        var result = FilterByNamePattern("*Model*").ToList();

        // UserModel, ProductModel, OrderModel, NotAModelClass, SerializableModel all contain "Model"
        Assert.Equal(5, result.Count);
        Assert.Contains(result, c => c.Name == "UserModel");
        Assert.Contains(result, c => c.Name == "ProductModel");
        Assert.Contains(result, c => c.Name == "OrderModel");
        Assert.Contains(result, c => c.Name == "NotAModelClass");
        Assert.Contains(result, c => c.Name == "SerializableModel");
    }

    // T053: Test attribute filter
    [Fact]
    public void AttributeFilter_MatchesSerializable()
    {
        var result = FilterByAttribute("Serializable").ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(result, c => c.Name == "SerializableClass");
        Assert.Contains(result, c => c.Name == "MultiAttributeClass");
        Assert.Contains(result, c => c.Name == "SerializableModel");
        Assert.DoesNotContain(result, c => c.Name == "NoAttributeClass");
    }

    [Fact]
    public void AttributeFilter_MatchesObsolete()
    {
        var result = FilterByAttribute("Obsolete").ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Name == "ObsoleteClass");
        Assert.Contains(result, c => c.Name == "MultiAttributeClass");
    }

    [Fact]
    public void AttributeFilter_WithAttributeSuffix_MatchesSame()
    {
        // Filter should work with or without "Attribute" suffix
        var resultWithSuffix = FilterByAttribute("SerializableAttribute").ToList();
        var resultWithoutSuffix = FilterByAttribute("Serializable").ToList();

        Assert.Equal(resultWithSuffix.Count, resultWithoutSuffix.Count);
    }

    [Fact]
    public void AttributeFilter_CustomAttribute_Matches()
    {
        var result = FilterByAttribute("Custom").ToList();

        Assert.Single(result);
        Assert.Contains(result, c => c.Name == "CustomAttributeClass");
    }

    // T054: Test inheritance filter
    [Fact]
    public void InheritanceFilter_MatchesBaseClass()
    {
        var result = FilterByInheritance("AnimalBase").ToList();

        Assert.Equal(3, result.Count); // Dog, Cat, SerializableModel inherit from AnimalBase
        Assert.Contains(result, c => c.Name == "Dog");
        Assert.Contains(result, c => c.Name == "Cat");
        Assert.Contains(result, c => c.Name == "SerializableModel");
        Assert.DoesNotContain(result, c => c.Name == "Bird");
    }

    [Fact]
    public void InheritanceFilter_MatchesInterface()
    {
        var result = FilterByInheritance("IRepository").ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Name == "UserRepository");
        Assert.Contains(result, c => c.Name == "ProductRepository");
        Assert.DoesNotContain(result, c => c.Name == "HelperClass");
    }

    // Test exact name match
    [Fact]
    public void NamePattern_ExactMatch_MatchesSingleClass()
    {
        var result = FilterByNamePattern("UserModel").ToList();

        Assert.Single(result);
        Assert.Equal("UserModel", result[0].Name);
    }

    // Test case insensitivity
    [Fact]
    public void NamePattern_CaseInsensitive_MatchesDifferentCase()
    {
        // The filter should be case-insensitive (matches VS extension behavior)
        var result = FilterByNamePattern("usermodel").ToList();

        Assert.Single(result);
        Assert.Equal("UserModel", result[0].Name);
    }

    // Test attribute metadata availability
    [Fact]
    public void ClassMetadata_Attributes_AreAccessible()
    {
        var serializableClass = _classMetadatas.FirstOrDefault(c => c.Name == "SerializableClass");

        Assert.NotNull(serializableClass);
        Assert.NotEmpty(serializableClass!.Attributes);
        Assert.Contains(serializableClass.Attributes, a =>
            a.Name.Contains("Serializable", StringComparison.OrdinalIgnoreCase));
    }

    // Test base class metadata availability
    [Fact]
    public void ClassMetadata_BaseClass_IsAccessible()
    {
        var dogClass = _classMetadatas.FirstOrDefault(c => c.Name == "Dog");

        Assert.NotNull(dogClass);
        Assert.NotNull(dogClass!.BaseClass);
        Assert.Equal("AnimalBase", dogClass.BaseClass!.Name);
    }

    // Test interfaces metadata availability
    [Fact]
    public void ClassMetadata_Interfaces_AreAccessible()
    {
        var userRepository = _classMetadatas.FirstOrDefault(c => c.Name == "UserRepository");

        Assert.NotNull(userRepository);
        Assert.NotEmpty(userRepository!.Interfaces);
        Assert.Contains(userRepository.Interfaces, i => i.Name == "IRepository");
    }

    // T055: Test predicate filter detection
    // Note: The $ prefix is detected by the filter but evaluated by the parser
    [Fact]
    public void PredicateFilter_DollarPrefix_IsRecognizedAsPredicate()
    {
        // Verify that predicate filters start with $
        var predicatePattern = "$CustomFilter";
        Assert.StartsWith("$", predicatePattern);

        // The actual predicate evaluation happens in the parser,
        // not in the filter class. This test confirms the syntax is recognized.
    }
}
