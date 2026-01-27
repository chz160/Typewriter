using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using Typewriter.CLI.CodeModel.Implementation;
using Typewriter.CLI.Configuration;
using Typewriter.CLI.Generation;
using Typewriter.CodeModel;
using Typewriter.Core.Abstractions;
using CodeModelFile = Typewriter.CodeModel.File;

namespace Typewriter.CLI.Tests;

/// <summary>
/// Base class for CLI parity tests providing utilities for template rendering,
/// Roslyn compilation, and code model creation.
/// </summary>
public abstract class CliParityTestBase
{
    #region Error Reporter

    /// <summary>
    /// Creates a mock error reporter for testing.
    /// </summary>
    protected static IErrorReporter CreateMockErrorReporter()
    {
        return Substitute.For<IErrorReporter>();
    }

    #endregion

    #region Roslyn Compilation Utilities

    /// <summary>
    /// Compiles C# source code and returns the semantic model.
    /// </summary>
    /// <param name="sourceCode">The C# source code to compile.</param>
    /// <returns>A tuple of compilation, syntax tree, and semantic model.</returns>
    protected static (Compilation compilation, SyntaxTree syntaxTree, SemanticModel semanticModel) CompileSource(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location),
        };

        // Add runtime assemblies
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        var additionalRefs = new[]
        {
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "netstandard.dll")),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references.Concat(additionalRefs),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        return (compilation, syntaxTree, semanticModel);
    }

    /// <summary>
    /// Gets a class symbol from compiled source code.
    /// </summary>
    /// <param name="sourceCode">The C# source code containing the class.</param>
    /// <param name="className">The name of the class to retrieve.</param>
    /// <returns>The named type symbol for the class.</returns>
    protected static INamedTypeSymbol? GetClassSymbol(string sourceCode, string className)
    {
        var (compilation, _, _) = CompileSource(sourceCode);
        return compilation.GetTypeByMetadataName(className);
    }

    #endregion

    #region Settings Utilities

    /// <summary>
    /// Creates default CLI settings for testing.
    /// </summary>
    /// <param name="strictNullGeneration">Whether to enable strict null generation.</param>
    /// <returns>A configured CliSettings instance.</returns>
    protected static CliSettings CreateTestSettings(bool strictNullGeneration = true)
    {
        var settings = new CliSettings(
            solutionFullName: @"C:\Test\TestSolution.sln",
            templatePath: @"C:\Test\Templates\Test.tst",
            log: null);

        if (!strictNullGeneration)
        {
            settings.DisableStrictNullGeneration();
        }

        return settings;
    }

    #endregion

    #region Type Metadata Utilities

    /// <summary>
    /// Creates type metadata from a type symbol.
    /// </summary>
    /// <param name="symbol">The type symbol.</param>
    /// <param name="settings">Optional settings for type resolution.</param>
    /// <returns>A type metadata instance.</returns>
    protected static Typewriter.Metadata.Interfaces.ITypeMetadata CreateTypeMetadata(ITypeSymbol symbol, CliSettings? settings = null)
    {
        settings ??= CreateTestSettings();
        return CliTypeMetadata.FromTypeSymbol(symbol, settings);
    }

    /// <summary>
    /// Gets a property's type symbol from a class.
    /// </summary>
    /// <param name="classSymbol">The class containing the property.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The property's type symbol.</returns>
    protected static ITypeSymbol? GetPropertyType(INamedTypeSymbol classSymbol, string propertyName)
    {
        var property = classSymbol.GetMembers(propertyName)
            .OfType<IPropertySymbol>()
            .FirstOrDefault();
        return property?.Type;
    }

    #endregion

    #region Template Rendering Utilities

    /// <summary>
    /// Renders a template string against a context object using CliParser.
    /// </summary>
    /// <param name="template">The template string to render.</param>
    /// <param name="context">The context object to render against.</param>
    /// <param name="templateInstance">Optional template instance for method invocation.</param>
    /// <param name="extensions">Optional extension types.</param>
    /// <returns>The rendered output string.</returns>
    protected static string RenderTemplate(
        string template,
        object context,
        object? templateInstance = null,
        List<System.Type>? extensions = null)
    {
        var errorReporter = CreateMockErrorReporter();
        var result = CliParser.Parse(
            templatePath: @"C:\Test\Template.tst",
            sourcePath: @"C:\Test\Source.cs",
            template: template,
            extensions: extensions ?? new List<System.Type>(),
            templateInstance: templateInstance,
            context: context,
            errorReporter: errorReporter,
            success: out _);

        return result ?? string.Empty;
    }

    /// <summary>
    /// Renders a template string against a file context using CliSingleFileParser.
    /// </summary>
    /// <param name="template">The template string to render.</param>
    /// <param name="file">The file context to render against.</param>
    /// <param name="templateInstance">Optional template instance for method invocation.</param>
    /// <param name="extensions">Optional extension types.</param>
    /// <returns>The rendered output string.</returns>
    protected static string RenderSingleFileTemplate(
        string template,
        CodeModelFile file,
        object? templateInstance = null,
        List<System.Type>? extensions = null)
    {
        var errorReporter = CreateMockErrorReporter();
        var result = CliSingleFileParser.Parse(
            templatePath: @"C:\Test\Template.tst",
            files: new[] { file },
            template: template,
            extensions: extensions ?? new List<System.Type>(),
            templateInstance: templateInstance,
            errorReporter: errorReporter,
            success: out _);

        return result ?? string.Empty;
    }

    #endregion

    #region Test Source Code Constants

    /// <summary>
    /// Standard test source code for type resolution tests.
    /// </summary>
    protected const string TypeResolutionTestSourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class TypeResolutionTestClass
    {
        // Primitive types
        public int IntProperty { get; set; }
        public string StringProperty { get; set; }
        public bool BoolProperty { get; set; }

        // Array types
        public int[] IntArrayProperty { get; set; }
        public string[] StringArrayProperty { get; set; }

        // Generic collection types
        public List<string> ListStringProperty { get; set; }
        public IEnumerable<int> IEnumerableIntProperty { get; set; }
        public ICollection<string> ICollectionStringProperty { get; set; }
        public HashSet<Guid> HashSetGuidProperty { get; set; }

        // Dictionary types
        public Dictionary<string, int> DictionaryStringIntProperty { get; set; }
        public IDictionary<string, object> IDictionaryProperty { get; set; }

        // Nullable types
        public int? NullableIntProperty { get; set; }
        public bool? NullableBoolProperty { get; set; }

        // Tuple types
        public (string Name, int Age) NamedTupleProperty { get; set; }
        public ValueTuple<string, int> ValueTupleProperty { get; set; }

        // Task types
        public Task<string> TaskStringProperty { get; set; }
        public Task TaskVoidProperty { get; set; }

        // Multi-argument generics
        public Func<string, int, bool> FuncThreeArgsProperty { get; set; }
        public Action<string, int> ActionTwoArgsProperty { get; set; }
    }

    // Circular reference test
    public class CircularReferenceClass
    {
        public string Name { get; set; }
        public CircularReferenceClass Parent { get; set; }
        public List<CircularReferenceClass> Children { get; set; }
    }
}";

    #endregion

    #region Mock Item Classes

    /// <summary>
    /// Base class for mock items used in testing.
    /// </summary>
    protected abstract class MockItem : Item
    {
        public string Name { get; set; } = string.Empty;
        public string name => string.IsNullOrEmpty(Name) ? string.Empty : char.ToLowerInvariant(Name[0]) + Name.Substring(1);
    }

    /// <summary>
    /// Mock class for testing.
    /// </summary>
    protected class MockClass : MockItem
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsAbstract { get; set; }
        public bool IsGeneric { get; set; }
        public bool IsEnum { get; set; }
        public MockPropertyCollection Properties { get; set; } = new();
        public MockMethodCollection Methods { get; set; } = new();
        public MockAttributeCollection Attributes { get; set; } = new();
    }

    /// <summary>
    /// Mock property for testing.
    /// </summary>
    protected class MockProperty : MockItem
    {
        public string Type { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public bool HasGetter { get; set; } = true;
        public bool HasSetter { get; set; } = true;
        public MockAttributeCollection Attributes { get; set; } = new();
    }

    /// <summary>
    /// Mock method for testing.
    /// </summary>
    protected class MockMethod : MockItem
    {
        public string Type { get; set; } = "void";
        public bool IsAbstract { get; set; }
        public bool IsVirtual { get; set; }
        public MockParameterCollection Parameters { get; set; } = new();
        public MockAttributeCollection Attributes { get; set; } = new();
    }

    /// <summary>
    /// Mock parameter for testing.
    /// </summary>
    protected class MockParameter : MockItem
    {
        public string Type { get; set; } = string.Empty;
        public bool HasDefaultValue { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// Mock attribute for testing.
    /// </summary>
    protected class MockAttribute : MockItem
    {
        public string FullName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Mock file for testing.
    /// </summary>
    protected class MockFile : MockItem
    {
        public string FullName { get; set; } = string.Empty;
        public MockClassCollection Classes { get; set; } = new();
        public MockInterfaceCollection Interfaces { get; set; } = new();
        public MockEnumCollection Enums { get; set; } = new();
    }

    /// <summary>
    /// Mock interface for testing.
    /// </summary>
    protected class MockInterface : MockItem
    {
        public string FullName { get; set; } = string.Empty;
        public MockPropertyCollection Properties { get; set; } = new();
        public MockMethodCollection Methods { get; set; } = new();
    }

    /// <summary>
    /// Mock enum for testing.
    /// </summary>
    protected class MockEnum : MockItem
    {
        public string FullName { get; set; } = string.Empty;
        public MockEnumValueCollection Values { get; set; } = new();
    }

    /// <summary>
    /// Mock enum value for testing.
    /// </summary>
    protected class MockEnumValue : MockItem
    {
        public long Value { get; set; }
    }

    #endregion

    #region Mock Collections

    protected class MockClassCollection : List<Item>, IFilterable
    {
        public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
            item => item is MockClass c ? c.Attributes.Select(a => a.Name) : Array.Empty<string>();

        public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> ItemFilterSelector =>
            item => item is MockClass c ? new[] { c.Name } : Array.Empty<string>();
    }

    protected class MockPropertyCollection : List<Item>, IFilterable
    {
        public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
            item => item is MockProperty p ? p.Attributes.Select(a => a.Name) : Array.Empty<string>();

        public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> ItemFilterSelector =>
            item => item is MockProperty p ? new[] { p.Name } : Array.Empty<string>();
    }

    protected class MockMethodCollection : List<Item>, IFilterable
    {
        public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
            item => item is MockMethod m ? m.Attributes.Select(a => a.Name) : Array.Empty<string>();

        public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> ItemFilterSelector =>
            item => item is MockMethod m ? new[] { m.Name } : Array.Empty<string>();
    }

    protected class MockParameterCollection : List<Item>, IFilterable
    {
        public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> ItemFilterSelector =>
            item => item is MockParameter p ? new[] { p.Name } : Array.Empty<string>();
    }

    protected class MockAttributeCollection : List<MockAttribute>, IFilterable
    {
        public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> ItemFilterSelector =>
            item => item is MockAttribute a ? new[] { a.Name } : Array.Empty<string>();
    }

    protected class MockInterfaceCollection : List<Item>, IFilterable
    {
        public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> ItemFilterSelector =>
            item => item is MockInterface i ? new[] { i.Name } : Array.Empty<string>();
    }

    protected class MockEnumCollection : List<Item>, IFilterable
    {
        public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> ItemFilterSelector =>
            item => item is MockEnum e ? new[] { e.Name } : Array.Empty<string>();
    }

    protected class MockEnumValueCollection : List<Item>, IFilterable
    {
        public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> ItemFilterSelector =>
            item => item is MockEnumValue v ? new[] { v.Name } : Array.Empty<string>();
    }

    #endregion

    #region Test Data Factories

    /// <summary>
    /// Creates a simple mock file with one class containing basic properties.
    /// </summary>
    protected static MockFile CreateSimpleModelFile()
    {
        var file = new MockFile
        {
            Name = "SimpleModel.cs",
            FullName = @"C:\Test\Models\SimpleModel.cs"
        };

        var simpleModel = new MockClass
        {
            Name = "SimpleModel",
            FullName = "TestFixtures.Models.SimpleModel"
        };

        simpleModel.Properties.Add(new MockProperty { Name = "Id", Type = "number" });
        simpleModel.Properties.Add(new MockProperty { Name = "Name", Type = "string" });
        simpleModel.Properties.Add(new MockProperty { Name = "IsActive", Type = "boolean" });

        file.Classes.Add(simpleModel);

        return file;
    }

    /// <summary>
    /// Creates a mock class with various property types for comprehensive testing.
    /// </summary>
    protected static MockClass CreateComprehensiveTestClass()
    {
        var testClass = new MockClass
        {
            Name = "ComprehensiveTestClass",
            FullName = "TestFixtures.ComprehensiveTestClass",
            IsAbstract = false,
            IsGeneric = false
        };

        // Add various properties
        testClass.Properties.Add(new MockProperty { Name = "Id", Type = "number", HasGetter = true, HasSetter = true });
        testClass.Properties.Add(new MockProperty { Name = "Name", Type = "string", HasGetter = true, HasSetter = true });
        testClass.Properties.Add(new MockProperty { Name = "ReadOnly", Type = "string", HasGetter = true, HasSetter = false });
        testClass.Properties.Add(new MockProperty { Name = "NullableValue", Type = "number", IsNullable = true });

        // Add methods
        testClass.Methods.Add(new MockMethod { Name = "DoSomething", Type = "void" });
        testClass.Methods.Add(new MockMethod { Name = "GetValue", Type = "string" });

        // Add attributes
        testClass.Attributes.Add(new MockAttribute { Name = "Serializable", FullName = "System.SerializableAttribute" });

        return testClass;
    }

    #endregion
}
