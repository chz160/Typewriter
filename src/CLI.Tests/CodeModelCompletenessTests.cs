using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Typewriter.CLI.CodeModel.Implementation;
using Typewriter.CLI.Configuration;
using Typewriter.Metadata.Interfaces;
using Xunit;

namespace Typewriter.CLI.Tests;

/// <summary>
/// Code model completeness parity tests for CLI template engine.
/// These tests verify that all code model properties are available and return correct values.
/// </summary>
public class CodeModelCompletenessTests
{
    private readonly CliSettings _settings;
    private readonly Compilation _compilation;
    private readonly IClassMetadata _testClass;
    private readonly IClassMetadata _staticClass;
    private readonly IEnumMetadata? _testEnum;
    private readonly IInterfaceMetadata? _testInterface;

    public CodeModelCompletenessTests()
    {
        _settings = new CliSettings("test.sln", "test.tst");

        // Compile the CodeModelTestSource fixture
        var sourceCode = GetCodeModelTestSourceCode();
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
        };

        // Add runtime assembly references
        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references = references.Concat(new[]
        {
            MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.ComponentModel.Primitives.dll")),
        }).ToArray();

        _compilation = CSharpCompilation.Create(
            "CodeModelTestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        // Get test class metadata
        _testClass = GetClassMetadata("CodeModelTestClass`2"); // Generic class with 2 type params
        _staticClass = GetClassMetadata("StaticCodeModelClass");
        _testEnum = GetEnumMetadatas().FirstOrDefault(e => e.Name == "CodeModelEnum");
        _testInterface = GetInterfaceMetadatas().FirstOrDefault(i => i.Name == "ICodeModelInterface");
    }

    private static string GetCodeModelTestSourceCode()
    {
        return @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeModelTests
{
    /// <summary>Interface for testing.</summary>
    public interface ICodeModelInterface
    {
        /// <summary>Interface property.</summary>
        string InterfaceProperty { get; set; }

        /// <summary>Interface method.</summary>
        void InterfaceMethod();

        /// <summary>Interface method with params.</summary>
        string InterfaceMethodWithParams(string param1, int param2);
    }

    /// <summary>Abstract base class.</summary>
    public abstract class CodeModelBaseClass
    {
        /// <summary>Base property.</summary>
        public int BaseProperty { get; set; }

        /// <summary>Abstract method.</summary>
        public abstract void AbstractMethod();

        /// <summary>Virtual method.</summary>
        public virtual void VirtualMethod() { }
    }

    /// <summary>
    /// Comprehensive test class for all code model properties.
    /// </summary>
    /// <typeparam name=""TKey"">Key type parameter.</typeparam>
    /// <typeparam name=""TValue"">Value type parameter.</typeparam>
    [Serializable]
    [Obsolete(""Test attribute with message"")]
    public class CodeModelTestClass<TKey, TValue> : CodeModelBaseClass, ICodeModelInterface
        where TKey : class
        where TValue : struct
    {
        // Fields
        /// <summary>Public field.</summary>
        public string PublicField;

        private int _privateField;

        /// <summary>Readonly field.</summary>
        public readonly string ReadonlyField = ""readonly"";

        /// <summary>Static field.</summary>
        public static int StaticField;

        /// <summary>Const field.</summary>
        public const string ConstField = ""const"";

        // Properties
        /// <summary>Read/write property.</summary>
        public string ReadWriteProperty { get; set; }

        /// <summary>Read-only property.</summary>
        public string ReadOnlyProperty { get; }

        /// <summary>Property with private setter.</summary>
        public string PrivateSetterProperty { get; private set; }

        /// <summary>Static property.</summary>
        public static string StaticProperty { get; set; }

        /// <summary>Property with default.</summary>
        public string PropertyWithDefault { get; set; } = ""default"";

        /// <summary>Property with attribute.</summary>
        [Obsolete(""Property is obsolete"")]
        public string AttributedProperty { get; set; }

        /// <summary>Nullable property.</summary>
        public int? NullableProperty { get; set; }

        /// <summary>Interface property.</summary>
        public string InterfaceProperty { get; set; }

        // Methods
        /// <summary>Simple method.</summary>
        public void SimpleMethod() { }

        /// <summary>Method with parameters.</summary>
        /// <param name=""param1"">First parameter.</param>
        /// <param name=""param2"">Second parameter.</param>
        public void MethodWithParams(string param1, int param2) { }

        /// <summary>Method with return value.</summary>
        /// <returns>A string.</returns>
        public string MethodWithReturn() => ""result"";

        /// <summary>Method with default parameter.</summary>
        public void MethodWithDefault(string value = ""default"") { }

        /// <summary>Static method.</summary>
        public static void StaticMethod() { }

        /// <summary>Generic method.</summary>
        /// <typeparam name=""T"">Method type parameter.</typeparam>
        public T GenericMethod<T>(T value) where T : class => value;

        /// <summary>Method with attribute.</summary>
        [Obsolete(""Method is obsolete"")]
        public void AttributedMethod() { }

        /// <summary>Async method.</summary>
        public async Task AsyncTaskMethod() => await Task.CompletedTask;

        /// <summary>Async method with return.</summary>
        public async Task<string> AsyncTaskWithReturnMethod() => await Task.FromResult(""result"");

        /// <summary>Abstract method implementation.</summary>
        public override void AbstractMethod() { }

        /// <summary>Virtual method override.</summary>
        public override void VirtualMethod() { }

        /// <summary>Interface method.</summary>
        public void InterfaceMethod() { }

        /// <summary>Interface method with params.</summary>
        public string InterfaceMethodWithParams(string param1, int param2) => $""{param1}-{param2}"";

        // Constructors
        /// <summary>Default constructor.</summary>
        public CodeModelTestClass() { }

        /// <summary>Constructor with param.</summary>
        public CodeModelTestClass(string readOnlyProperty) => ReadOnlyProperty = readOnlyProperty;
    }

    /// <summary>Static class for testing.</summary>
    public static class StaticCodeModelClass
    {
        /// <summary>Static property.</summary>
        public static string StaticProperty { get; set; }

        /// <summary>Static method.</summary>
        public static void StaticMethod() { }

        /// <summary>Extension method.</summary>
        public static string ToCustomString(this string value) => $""Custom: {value}"";
    }

    /// <summary>Enum for testing.</summary>
    public enum CodeModelEnum
    {
        /// <summary>First value.</summary>
        First = 0,

        /// <summary>Second value.</summary>
        Second = 1,

        /// <summary>Third value.</summary>
        Third = 10,

        /// <summary>Fourth value.</summary>
        [Obsolete(""Deprecated"")]
        Fourth = 20
    }

    /// <summary>Flags enum for testing.</summary>
    [Flags]
    public enum CodeModelFlagsEnum
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
        All = Read | Write | Execute
    }
}
";
    }

    private IClassMetadata GetClassMetadata(string className)
    {
        foreach (var syntaxTree in _compilation.SyntaxTrees)
        {
            var semanticModel = _compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            var classDeclarations = root.DescendantNodes()
                .Where(n => n is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax);

            foreach (var classDecl in classDeclarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                if (symbol != null && symbol.MetadataName == className)
                {
                    return CliClassMetadata.FromNamedTypeSymbol(symbol, _settings)!;
                }
            }
        }

        throw new InvalidOperationException($"Class {className} not found");
    }

    private IEnumerable<IEnumMetadata> GetEnumMetadatas()
    {
        var symbols = new List<INamedTypeSymbol>();

        foreach (var syntaxTree in _compilation.SyntaxTrees)
        {
            var semanticModel = _compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            var enumDeclarations = root.DescendantNodes()
                .Where(n => n is Microsoft.CodeAnalysis.CSharp.Syntax.EnumDeclarationSyntax);

            foreach (var enumDecl in enumDeclarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(enumDecl) as INamedTypeSymbol;
                if (symbol != null)
                {
                    symbols.Add(symbol);
                }
            }
        }

        return CliEnumMetadata.FromNamedTypeSymbols(symbols, _settings);
    }

    private IEnumerable<IInterfaceMetadata> GetInterfaceMetadatas()
    {
        var symbols = new List<INamedTypeSymbol>();

        foreach (var syntaxTree in _compilation.SyntaxTrees)
        {
            var semanticModel = _compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            var interfaceDeclarations = root.DescendantNodes()
                .Where(n => n is Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax);

            foreach (var interfaceDecl in interfaceDeclarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(interfaceDecl) as INamedTypeSymbol;
                if (symbol != null)
                {
                    symbols.Add(symbol);
                }
            }
        }

        return CliInterfaceMetadata.FromNamedTypeSymbols(symbols, null, _settings);
    }

    // T061: Test class properties
    [Fact]
    public void Class_Name_IsCorrect()
    {
        Assert.Equal("CodeModelTestClass", _testClass.Name);
    }

    [Fact]
    public void Class_FullName_ContainsNamespace()
    {
        Assert.Contains("CodeModelTests.CodeModelTestClass", _testClass.FullName);
    }

    [Fact]
    public void Class_Namespace_IsCorrect()
    {
        Assert.Equal("CodeModelTests", _testClass.Namespace);
    }

    [Fact]
    public void Class_IsAbstract_IsFalse()
    {
        Assert.False(_testClass.IsAbstract);
    }

    [Fact]
    public void Class_IsGeneric_IsTrue()
    {
        Assert.True(_testClass.IsGeneric);
    }

    [Fact]
    public void Class_TypeParameters_HasTwoParameters()
    {
        Assert.Equal(2, _testClass.TypeParameters.Count());
        Assert.Contains(_testClass.TypeParameters, tp => tp.Name == "TKey");
        Assert.Contains(_testClass.TypeParameters, tp => tp.Name == "TValue");
    }

    [Fact]
    public void Class_BaseClass_IsCorrect()
    {
        Assert.NotNull(_testClass.BaseClass);
        Assert.Equal("CodeModelBaseClass", _testClass.BaseClass!.Name);
    }

    [Fact]
    public void Class_Interfaces_ContainsInterface()
    {
        Assert.Contains(_testClass.Interfaces, i => i.Name == "ICodeModelInterface");
    }

    [Fact]
    public void Class_Attributes_HasAttributes()
    {
        Assert.NotEmpty(_testClass.Attributes);
        Assert.Contains(_testClass.Attributes, a => a.Name.Contains("Serializable"));
        Assert.Contains(_testClass.Attributes, a => a.Name.Contains("Obsolete"));
    }

    [Fact]
    public void StaticClass_IsStatic_IsTrue()
    {
        Assert.True(_staticClass.IsStatic);
    }

    // T062: Test property properties
    [Fact]
    public void Property_HasGetter_IsTrue()
    {
        var prop = _testClass.Properties.FirstOrDefault(p => p.Name == "ReadWriteProperty");
        Assert.NotNull(prop);
        Assert.True(prop!.HasGetter);
    }

    [Fact]
    public void Property_HasSetter_IsCorrect()
    {
        var readWrite = _testClass.Properties.FirstOrDefault(p => p.Name == "ReadWriteProperty");
        var readOnly = _testClass.Properties.FirstOrDefault(p => p.Name == "ReadOnlyProperty");

        Assert.NotNull(readWrite);
        Assert.NotNull(readOnly);
        Assert.True(readWrite!.HasSetter);
        Assert.False(readOnly!.HasSetter);
    }

    // Note: Static properties are filtered out in CliPropertyMetadata.FromPropertySymbols
    // This matches the VS extension behavior which only exposes instance properties
    [Fact]
    public void Property_StaticProperties_AreFilteredOut()
    {
        var staticProp = _testClass.Properties.FirstOrDefault(p => p.Name == "StaticProperty");
        // Static properties are not included in the Properties collection
        Assert.Null(staticProp);
    }

    [Fact]
    public void Property_Type_IsCorrect()
    {
        var prop = _testClass.Properties.FirstOrDefault(p => p.Name == "NullableProperty");
        Assert.NotNull(prop);
        Assert.NotNull(prop!.Type);
        Assert.True(prop.Type.IsNullable);
    }

    [Fact]
    public void Property_Attributes_AreAccessible()
    {
        var prop = _testClass.Properties.FirstOrDefault(p => p.Name == "AttributedProperty");
        Assert.NotNull(prop);
        Assert.NotEmpty(prop!.Attributes);
        Assert.Contains(prop.Attributes, a => a.Name.Contains("Obsolete"));
    }

    // T063: Test method properties
    [Fact]
    public void Method_Name_IsCorrect()
    {
        var method = _testClass.Methods.FirstOrDefault(m => m.Name == "SimpleMethod");
        Assert.NotNull(method);
        Assert.Equal("SimpleMethod", method!.Name);
    }

    [Fact]
    public void Method_Parameters_AreAccessible()
    {
        var method = _testClass.Methods.FirstOrDefault(m => m.Name == "MethodWithParams");
        Assert.NotNull(method);
        Assert.Equal(2, method!.Parameters.Count());
        Assert.Contains(method.Parameters, p => p.Name == "param1");
        Assert.Contains(method.Parameters, p => p.Name == "param2");
    }

    [Fact]
    public void Method_Parameters_Types_AreCorrect()
    {
        var method = _testClass.Methods.FirstOrDefault(m => m.Name == "MethodWithParams");
        Assert.NotNull(method);

        var param1 = method!.Parameters.FirstOrDefault(p => p.Name == "param1");
        var param2 = method.Parameters.FirstOrDefault(p => p.Name == "param2");

        Assert.NotNull(param1);
        Assert.NotNull(param2);
        Assert.True(((CliTypeMetadata)param1!.Type).IsString);
        Assert.True(((CliTypeMetadata)param2!.Type).IsPrimitive);
    }

    [Fact]
    public void Method_ReturnType_IsCorrect()
    {
        var method = _testClass.Methods.FirstOrDefault(m => m.Name == "MethodWithReturn");
        Assert.NotNull(method);
        Assert.NotNull(method!.Type);
        Assert.True(((CliTypeMetadata)method.Type).IsString);
    }

    // Note: Static methods are filtered out in CliMethodMetadata.FromMethodSymbols
    // This matches the VS extension behavior which only exposes instance methods
    [Fact]
    public void Method_StaticMethods_AreFilteredOut()
    {
        var staticMethod = _testClass.Methods.FirstOrDefault(m => m.Name == "StaticMethod");
        // Static methods are not included in the Methods collection
        Assert.Null(staticMethod);
    }

    [Fact]
    public void Method_IsGeneric_IsCorrect()
    {
        var nonGeneric = _testClass.Methods.FirstOrDefault(m => m.Name == "SimpleMethod");
        var generic = _testClass.Methods.FirstOrDefault(m => m.Name == "GenericMethod");

        Assert.NotNull(nonGeneric);
        Assert.NotNull(generic);
        Assert.False(nonGeneric!.IsGeneric);
        Assert.True(generic!.IsGeneric);
    }

    [Fact]
    public void Method_TypeParameters_AreAccessible()
    {
        var method = _testClass.Methods.FirstOrDefault(m => m.Name == "GenericMethod");
        Assert.NotNull(method);
        Assert.Single(method!.TypeParameters);
        Assert.Equal("T", method.TypeParameters.First().Name);
    }

    [Fact]
    public void Method_Attributes_AreAccessible()
    {
        var method = _testClass.Methods.FirstOrDefault(m => m.Name == "AttributedMethod");
        Assert.NotNull(method);
        Assert.NotEmpty(method!.Attributes);
        Assert.Contains(method.Attributes, a => a.Name.Contains("Obsolete"));
    }

    [Fact]
    public void Method_DefaultParameters_AreAccessible()
    {
        var method = _testClass.Methods.FirstOrDefault(m => m.Name == "MethodWithDefault");
        Assert.NotNull(method);

        var param = method!.Parameters.FirstOrDefault();
        Assert.NotNull(param);
        Assert.True(param!.HasDefaultValue);
    }

    // T064: Test attribute properties
    [Fact]
    public void Attribute_Name_IsCorrect()
    {
        var attr = _testClass.Attributes.FirstOrDefault(a => a.Name.Contains("Obsolete"));
        Assert.NotNull(attr);
        Assert.Contains("Obsolete", attr!.Name);
    }

    [Fact]
    public void Attribute_FullName_ContainsNamespace()
    {
        var attr = _testClass.Attributes.FirstOrDefault(a => a.Name.Contains("Serializable"));
        Assert.NotNull(attr);
        Assert.Contains("System", attr!.FullName);
    }

    [Fact]
    public void Attribute_Arguments_AreAccessible()
    {
        var attr = _testClass.Attributes.FirstOrDefault(a => a.Name.Contains("Obsolete"));
        Assert.NotNull(attr);
        // Obsolete has a message argument
        Assert.NotEmpty(attr!.Arguments);
    }

    // T065: Test DocComment access
    [Fact]
    public void Class_DocComment_IsAccessible()
    {
        Assert.NotNull(_testClass.DocComment);
        Assert.Contains("Comprehensive test class", _testClass.DocComment);
    }

    [Fact]
    public void Property_DocComment_IsAccessible()
    {
        var prop = _testClass.Properties.FirstOrDefault(p => p.Name == "ReadWriteProperty");
        Assert.NotNull(prop);
        Assert.NotNull(prop!.DocComment);
        Assert.Contains("Read/write property", prop.DocComment);
    }

    [Fact]
    public void Method_DocComment_IsAccessible()
    {
        var method = _testClass.Methods.FirstOrDefault(m => m.Name == "SimpleMethod");
        Assert.NotNull(method);
        Assert.NotNull(method!.DocComment);
        Assert.Contains("Simple method", method.DocComment);
    }

    // Interface tests
    [Fact]
    public void Interface_Name_IsCorrect()
    {
        Assert.Equal("ICodeModelInterface", _testInterface.Name);
    }

    [Fact]
    public void Interface_Properties_AreAccessible()
    {
        Assert.Single(_testInterface.Properties);
        Assert.Equal("InterfaceProperty", _testInterface.Properties.First().Name);
    }

    [Fact]
    public void Interface_Methods_AreAccessible()
    {
        Assert.Equal(2, _testInterface.Methods.Count());
        Assert.Contains(_testInterface.Methods, m => m.Name == "InterfaceMethod");
        Assert.Contains(_testInterface.Methods, m => m.Name == "InterfaceMethodWithParams");
    }

    // Enum tests
    [Fact]
    public void Enum_Name_IsCorrect()
    {
        Assert.Equal("CodeModelEnum", _testEnum.Name);
    }

    [Fact]
    public void Enum_Values_AreAccessible()
    {
        Assert.Equal(4, _testEnum.Values.Count());
        Assert.Contains(_testEnum.Values, v => v.Name == "First");
        Assert.Contains(_testEnum.Values, v => v.Name == "Second");
        Assert.Contains(_testEnum.Values, v => v.Name == "Third");
        Assert.Contains(_testEnum.Values, v => v.Name == "Fourth");
    }

    [Fact]
    public void EnumValue_Value_IsCorrect()
    {
        var third = _testEnum.Values.FirstOrDefault(v => v.Name == "Third");
        Assert.NotNull(third);
        Assert.Equal(10, third!.Value);
    }

    [Fact]
    public void EnumValue_Attributes_AreAccessible()
    {
        var fourth = _testEnum.Values.FirstOrDefault(v => v.Name == "Fourth");
        Assert.NotNull(fourth);
        Assert.NotEmpty(fourth!.Attributes);
        Assert.Contains(fourth.Attributes, a => a.Name.Contains("Obsolete"));
    }

    // Field tests
    [Fact]
    public void Class_Fields_AreAccessible()
    {
        var fields = _testClass.Fields.ToList();
        Assert.NotEmpty(fields);
        Assert.Contains(fields, f => f.Name == "PublicField");
    }

    [Fact]
    public void Class_Constants_AreAccessible()
    {
        var constants = _testClass.Constants.ToList();
        Assert.NotEmpty(constants);
        Assert.Contains(constants, c => c.Name == "ConstField");
    }

    [Fact]
    public void Constant_Value_IsCorrect()
    {
        var constField = _testClass.Constants.FirstOrDefault(c => c.Name == "ConstField");
        Assert.NotNull(constField);
        Assert.Equal("const", constField!.Value);
    }
}
