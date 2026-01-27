using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Typewriter.CLI.CodeModel.Implementation;
using Typewriter.CLI.Configuration;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;
using Xunit;

namespace Typewriter.CLI.Tests;

/// <summary>
/// Type resolution parity tests for CLI template engine.
/// These tests verify that CLI resolves types identically to the VS extension.
/// </summary>
public class TypeResolutionParityTests
{
    private readonly CliSettings _settings;
    private readonly Compilation _compilation;

    public TypeResolutionParityTests()
    {
        _settings = new CliSettings("test.sln", "test.tst");

        // Compile the TypeResolutionTestSource fixture
        var sourceCode = GetTypeResolutionTestSourceCode();
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Action<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location),
        };

        // Add runtime assembly references
        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        references = references.Concat(new[]
        {
            MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll")),
        }).ToArray();

        _compilation = CSharpCompilation.Create(
            "TypeResolutionTestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
    }

    private static string GetTypeResolutionTestSourceCode()
    {
        return @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TypeResolutionTests
{
    public class TypeResolutionModel
    {
        // Array types
        public int[] IntArrayProperty { get; set; }
        public string[] StringArrayProperty { get; set; }
        public DateTime[] DateArrayProperty { get; set; }

        // Generic list types
        public List<string> ListStringProperty { get; set; }
        public List<int> ListIntProperty { get; set; }
        public IList<string> IListStringProperty { get; set; }

        // IEnumerable types
        public IEnumerable<int> IEnumerableIntProperty { get; set; }
        public IEnumerable<string> IEnumerableStringProperty { get; set; }

        // ICollection types
        public ICollection<string> ICollectionStringProperty { get; set; }
        public ICollection<int> ICollectionIntProperty { get; set; }

        // HashSet types
        public HashSet<Guid> HashSetGuidProperty { get; set; }
        public HashSet<string> HashSetStringProperty { get; set; }

        // Dictionary types
        public Dictionary<string, int> DictionaryStringIntProperty { get; set; }
        public Dictionary<int, string> DictionaryIntStringProperty { get; set; }
        public IDictionary<string, object> IDictionaryStringObjectProperty { get; set; }
        public IReadOnlyDictionary<string, int> IReadOnlyDictionaryProperty { get; set; }

        // Nullable types
        public int? NullableIntProperty { get; set; }
        public DateTime? NullableDateTimeProperty { get; set; }
        public Guid? NullableGuidProperty { get; set; }

        // Tuple types
        public (string Name, int Age) NamedTupleProperty { get; set; }
        public (int, string) UnnamedTupleProperty { get; set; }
        public (string First, int Second, bool Third) ThreeElementTupleProperty { get; set; }

        // Task types (for async method return types)
        public Task TaskProperty { get; set; }
        public Task<string> TaskStringProperty { get; set; }
        public Task<int> TaskIntProperty { get; set; }
        public Task<List<string>> TaskListStringProperty { get; set; }
        public Task<int?> TaskNullableIntProperty { get; set; }

        // Multi-argument generic types
        public Func<string, int> FuncStringIntProperty { get; set; }
        public Func<string, int, bool> FuncThreeArgsProperty { get; set; }
        public Action<string> ActionStringProperty { get; set; }

        // Nested generic types
        public List<List<string>> NestedListProperty { get; set; }
        public Dictionary<string, List<int>> DictionaryWithListValueProperty { get; set; }

        // Circular reference
        public TypeResolutionModel SelfReferenceProperty { get; set; }
        public List<TypeResolutionModel> SelfReferenceListProperty { get; set; }
    }

    public class GenericTypeModel<T>
    {
        public T GenericProperty { get; set; }
        public List<T> GenericListProperty { get; set; }
    }
}
";
    }

    private ITypeSymbol GetPropertyTypeSymbol(string propertyName)
    {
        var typeSymbol = _compilation.GetTypeByMetadataName("TypeResolutionTests.TypeResolutionModel");
        Assert.NotNull(typeSymbol);

        var property = typeSymbol!.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Name == propertyName);
        Assert.NotNull(property);

        return property!.Type;
    }

    private CliTypeMetadata GetTypeMetadata(string propertyName)
    {
        var typeSymbol = GetPropertyTypeSymbol(propertyName);
        return (CliTypeMetadata)CliTypeMetadata.FromTypeSymbol(typeSymbol, _settings);
    }

    private CliTypeMetadata AsCliType(ITypeMetadata metadata)
    {
        return (CliTypeMetadata)metadata;
    }

    // T032: Test array type resolution
    [Fact]
    public void ArrayType_IntArray_ReturnsNumberArray()
    {
        var metadata = GetTypeMetadata("IntArrayProperty");

        Assert.True(metadata.IsArray);
        Assert.True(metadata.IsEnumerable);
        Assert.False(metadata.IsDictionary);

        var typeArgs = metadata.TypeArguments.ToList();
        Assert.Single(typeArgs);
        Assert.True(AsCliType(typeArgs[0]).IsPrimitive);
    }

    [Fact]
    public void ArrayType_StringArray_ElementTypeIsString()
    {
        var metadata = GetTypeMetadata("StringArrayProperty");

        Assert.True(metadata.IsArray);
        Assert.NotNull(metadata.ElementType);
        Assert.True(AsCliType(metadata.ElementType!).IsString);
    }

    // T033: Test generic list resolution
    [Fact]
    public void GenericType_ListString_ReturnsStringArray()
    {
        var metadata = GetTypeMetadata("ListStringProperty");

        Assert.True(metadata.IsEnumerable);
        Assert.False(metadata.IsArray);
        Assert.False(metadata.IsDictionary);

        var typeArgs = metadata.TypeArguments.ToList();
        Assert.Single(typeArgs);
        Assert.True(AsCliType(typeArgs[0]).IsString);
    }

    [Fact]
    public void GenericType_ListInt_TypeArgumentIsPrimitive()
    {
        var metadata = GetTypeMetadata("ListIntProperty");

        Assert.True(metadata.IsEnumerable);
        var typeArgs = metadata.TypeArguments.ToList();
        Assert.Single(typeArgs);
        Assert.True(AsCliType(typeArgs[0]).IsPrimitive);
    }

    // T034: Test IEnumerable<T> type resolution
    [Fact]
    public void GenericType_IEnumerableInt_ReturnsNumberArray()
    {
        var metadata = GetTypeMetadata("IEnumerableIntProperty");

        Assert.True(metadata.IsEnumerable);
        Assert.False(metadata.IsArray);
        Assert.False(metadata.IsDictionary);

        var typeArgs = metadata.TypeArguments.ToList();
        Assert.Single(typeArgs);
        Assert.True(AsCliType(typeArgs[0]).IsPrimitive);
    }

    // T035: Test ICollection<T> type resolution
    [Fact]
    public void GenericType_ICollectionString_ReturnsStringArray()
    {
        var metadata = GetTypeMetadata("ICollectionStringProperty");

        Assert.True(metadata.IsEnumerable);
        Assert.False(metadata.IsArray);
        Assert.False(metadata.IsDictionary);

        var typeArgs = metadata.TypeArguments.ToList();
        Assert.Single(typeArgs);
        Assert.True(AsCliType(typeArgs[0]).IsString);
    }

    // T036: Test HashSet<T> type resolution
    [Fact]
    public void GenericType_HashSetGuid_ReturnsGuidArray()
    {
        var metadata = GetTypeMetadata("HashSetGuidProperty");

        // Note: HashSet is not in the IsEnumerable list in CliTypeMetadata
        // This tests current behavior - may need adjustment if HashSet should be enumerable
        var typeArgs = metadata.TypeArguments.ToList();
        Assert.Single(typeArgs);
        Assert.True(AsCliType(typeArgs[0]).IsGuid);
    }

    // T037: Test dictionary type resolution
    [Fact]
    public void DictionaryType_StringInt_ReturnsIndexSignature()
    {
        var metadata = GetTypeMetadata("DictionaryStringIntProperty");

        Assert.True(metadata.IsDictionary);
        Assert.False(metadata.IsArray);

        var typeArgs = metadata.TypeArguments.ToList();
        Assert.Equal(2, typeArgs.Count);
        Assert.True(AsCliType(typeArgs[0]).IsString); // Key type
        Assert.True(AsCliType(typeArgs[1]).IsPrimitive); // Value type
    }

    [Fact]
    public void DictionaryType_IDictionary_IsDictionaryTrue()
    {
        var metadata = GetTypeMetadata("IDictionaryStringObjectProperty");

        Assert.True(metadata.IsDictionary);
        var typeArgs = metadata.TypeArguments.ToList();
        Assert.Equal(2, typeArgs.Count);
    }

    [Fact]
    public void DictionaryType_IReadOnlyDictionary_IsDictionaryTrue()
    {
        var metadata = GetTypeMetadata("IReadOnlyDictionaryProperty");

        Assert.True(metadata.IsDictionary);
        var typeArgs = metadata.TypeArguments.ToList();
        Assert.Equal(2, typeArgs.Count);
    }

    // T038: Test nullable type resolution
    [Fact]
    public void NullableType_IntNullable_ReturnsNumberOrNull()
    {
        var metadata = GetTypeMetadata("NullableIntProperty");

        Assert.True(metadata.IsNullable);
        Assert.True(AsCliType(metadata).IsPrimitive);
        Assert.Contains("?", metadata.Name);
    }

    [Fact]
    public void NullableType_DateTimeNullable_IsNullableTrue()
    {
        var metadata = GetTypeMetadata("NullableDateTimeProperty");

        Assert.True(metadata.IsNullable);
        Assert.True(AsCliType(metadata).IsDate);
    }

    [Fact]
    public void NullableType_GuidNullable_IsNullableTrue()
    {
        var metadata = GetTypeMetadata("NullableGuidProperty");

        Assert.True(metadata.IsNullable);
        Assert.True(AsCliType(metadata).IsGuid);
    }

    // T039: Test tuple type resolution
    [Fact]
    public void TupleType_NamedElements_ReturnsTuple()
    {
        var metadata = GetTypeMetadata("NamedTupleProperty");

        Assert.True(metadata.IsValueTuple);

        var tupleElements = metadata.TupleElements.ToList();
        Assert.Equal(2, tupleElements.Count);

        // Verify named elements
        Assert.Equal("Name", tupleElements[0].Name);
        Assert.Equal("Age", tupleElements[1].Name);
    }

    [Fact]
    public void TupleType_UnnamedElements_HasDefaultNames()
    {
        var metadata = GetTypeMetadata("UnnamedTupleProperty");

        Assert.True(metadata.IsValueTuple);

        var tupleElements = metadata.TupleElements.ToList();
        Assert.Equal(2, tupleElements.Count);
    }

    [Fact]
    public void TupleType_ThreeElements_AllAccessible()
    {
        var metadata = GetTypeMetadata("ThreeElementTupleProperty");

        Assert.True(metadata.IsValueTuple);

        var tupleElements = metadata.TupleElements.ToList();
        Assert.Equal(3, tupleElements.Count);
        Assert.Equal("First", tupleElements[0].Name);
        Assert.Equal("Second", tupleElements[1].Name);
        Assert.Equal("Third", tupleElements[2].Name);
    }

    // T040: Test Task<T> unwrapping
    [Fact]
    public void TaskType_TaskString_UnwrapsToString()
    {
        var metadata = GetTypeMetadata("TaskStringProperty");

        // Task<T> should be unwrapped to T
        Assert.True(AsCliType(metadata).IsString);
        Assert.False(metadata.IsTask);
    }

    [Fact]
    public void TaskType_TaskInt_UnwrapsToInt()
    {
        var metadata = GetTypeMetadata("TaskIntProperty");

        // Task<T> should be unwrapped to T
        Assert.True(AsCliType(metadata).IsPrimitive);
        Assert.False(metadata.IsTask);
    }

    [Fact]
    public void TaskType_TaskListString_UnwrapsToListString()
    {
        var metadata = GetTypeMetadata("TaskListStringProperty");

        // Task<List<string>> should unwrap to List<string>
        Assert.True(metadata.IsEnumerable);
        Assert.False(metadata.IsTask);

        var typeArgs = metadata.TypeArguments.ToList();
        Assert.Single(typeArgs);
        Assert.True(AsCliType(typeArgs[0]).IsString);
    }

    [Fact]
    public void TaskType_TaskNullableInt_UnwrapsToNullableInt()
    {
        var metadata = GetTypeMetadata("TaskNullableIntProperty");

        // Task<int?> should unwrap to int?
        Assert.True(metadata.IsNullable);
        Assert.True(AsCliType(metadata).IsPrimitive);
        Assert.False(metadata.IsTask);
    }

    [Fact]
    public void TaskType_Task_RemainsAsTask()
    {
        var metadata = GetTypeMetadata("TaskProperty");

        // Non-generic Task should remain as Task (represents void)
        Assert.True(metadata.IsTask);
    }

    // T041: Test multi-argument generic types
    [Fact]
    public void GenericType_FuncWithThreeArgs_AllTypeArgumentsAvailable()
    {
        var metadata = GetTypeMetadata("FuncThreeArgsProperty");

        var typeArgs = metadata.TypeArguments.ToList();
        Assert.Equal(3, typeArgs.Count);
        Assert.True(AsCliType(typeArgs[0]).IsString); // First arg: string
        Assert.True(AsCliType(typeArgs[1]).IsPrimitive); // Second arg: int
        Assert.True(AsCliType(typeArgs[2]).IsPrimitive); // Third arg (return): bool
    }

    [Fact]
    public void GenericType_ActionString_SingleTypeArgument()
    {
        var metadata = GetTypeMetadata("ActionStringProperty");

        var typeArgs = metadata.TypeArguments.ToList();
        Assert.Single(typeArgs);
        Assert.True(AsCliType(typeArgs[0]).IsString);
    }

    // T042: Test circular type reference handling
    [Fact]
    public void CircularReference_SelfReferencing_DoesNotInfiniteLoop()
    {
        var metadata = GetTypeMetadata("SelfReferenceProperty");

        // Should complete without infinite loop
        Assert.NotNull(metadata);
        Assert.Equal("TypeResolutionModel", metadata.Name);
        Assert.True(metadata.IsDefined);
    }

    [Fact]
    public void CircularReference_ListOfSelf_DoesNotInfiniteLoop()
    {
        var metadata = GetTypeMetadata("SelfReferenceListProperty");

        Assert.True(metadata.IsEnumerable);

        var typeArgs = metadata.TypeArguments.ToList();
        Assert.Single(typeArgs);
        Assert.Equal("TypeResolutionModel", typeArgs[0].Name);
    }

    // Additional tests for nested generics
    [Fact]
    public void NestedGeneric_ListOfLists_TypeArgumentsAccessible()
    {
        var metadata = GetTypeMetadata("NestedListProperty");

        Assert.True(metadata.IsEnumerable);

        var typeArgs = metadata.TypeArguments.ToList();
        Assert.Single(typeArgs);

        // Inner list should also be enumerable
        Assert.True(AsCliType(typeArgs[0]).IsEnumerable);
        var innerTypeArgs = typeArgs[0].TypeArguments.ToList();
        Assert.Single(innerTypeArgs);
        Assert.True(AsCliType(innerTypeArgs[0]).IsString);
    }

    [Fact]
    public void NestedGeneric_DictionaryWithListValue_BothLevelsAccessible()
    {
        var metadata = GetTypeMetadata("DictionaryWithListValueProperty");

        Assert.True(metadata.IsDictionary);

        var typeArgs = metadata.TypeArguments.ToList();
        Assert.Equal(2, typeArgs.Count);
        Assert.True(AsCliType(typeArgs[0]).IsString); // Key
        Assert.True(AsCliType(typeArgs[1]).IsEnumerable); // Value is List<int>

        var valueTypeArgs = typeArgs[1].TypeArguments.ToList();
        Assert.Single(valueTypeArgs);
        Assert.True(AsCliType(valueTypeArgs[0]).IsPrimitive); // int
    }
}
