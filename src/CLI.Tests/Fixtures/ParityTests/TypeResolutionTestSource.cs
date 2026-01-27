// Test fixture for type resolution parity tests
// This file provides C# types for testing CLI type resolution against VS extension behavior

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Typewriter.CLI.Tests.Fixtures.ParityTests
{
    /// <summary>
    /// Test class containing various property types for type resolution testing.
    /// </summary>
    public class TypeResolutionTestSource
    {
        // Primitive types
        public int IntProperty { get; set; }
        public string StringProperty { get; set; }
        public bool BoolProperty { get; set; }
        public double DoubleProperty { get; set; }
        public decimal DecimalProperty { get; set; }
        public Guid GuidProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
        public TimeSpan TimeSpanProperty { get; set; }

        // Array types
        public int[] IntArrayProperty { get; set; }
        public string[] StringArrayProperty { get; set; }
        public byte[] ByteArrayProperty { get; set; }

        // Generic collection types
        public List<string> ListStringProperty { get; set; }
        public List<int> ListIntProperty { get; set; }
        public IEnumerable<int> IEnumerableIntProperty { get; set; }
        public ICollection<string> ICollectionStringProperty { get; set; }
        public HashSet<Guid> HashSetGuidProperty { get; set; }
        public IList<double> IListDoubleProperty { get; set; }
        public IReadOnlyList<string> IReadOnlyListStringProperty { get; set; }
        public IReadOnlyCollection<int> IReadOnlyCollectionIntProperty { get; set; }

        // Dictionary types
        public Dictionary<string, int> DictionaryStringIntProperty { get; set; }
        public Dictionary<int, string> DictionaryIntStringProperty { get; set; }
        public IDictionary<string, object> IDictionaryStringObjectProperty { get; set; }
        public IReadOnlyDictionary<string, int> IReadOnlyDictionaryProperty { get; set; }

        // Nullable types
        public int? NullableIntProperty { get; set; }
        public bool? NullableBoolProperty { get; set; }
        public DateTime? NullableDateTimeProperty { get; set; }
        public Guid? NullableGuidProperty { get; set; }

        // Tuple types
        public (string Name, int Age) NamedTupleProperty { get; set; }
        public (string, int) UnnamedTupleProperty { get; set; }
        public (string First, int Second, bool Third) ThreeElementTupleProperty { get; set; }
        public ValueTuple<string, int> ValueTupleProperty { get; set; }

        // Task types (for async method return types)
        public Task<string> TaskStringProperty { get; set; }
        public Task<int> TaskIntProperty { get; set; }
        public Task<List<string>> TaskListStringProperty { get; set; }
        public Task TaskVoidProperty { get; set; }

        // Multi-argument generic types
        public Func<string, int> FuncStringIntProperty { get; set; }
        public Func<string, int, bool> FuncThreeArgsProperty { get; set; }
        public Action<string> ActionStringProperty { get; set; }
        public Action<string, int, bool> ActionThreeArgsProperty { get; set; }
        public KeyValuePair<string, int> KeyValuePairProperty { get; set; }

        // Nested generics
        public List<List<int>> NestedListProperty { get; set; }
        public Dictionary<string, List<int>> DictionaryWithListValueProperty { get; set; }
        public List<Dictionary<string, int>> ListOfDictionariesProperty { get; set; }

        // Dynamic and object
        public dynamic DynamicProperty { get; set; }
        public object ObjectProperty { get; set; }
    }

    /// <summary>
    /// Test class for circular reference handling.
    /// </summary>
    public class CircularReferenceTestSource
    {
        public string Name { get; set; }
        public CircularReferenceTestSource Parent { get; set; }
        public List<CircularReferenceTestSource> Children { get; set; }
    }

    /// <summary>
    /// Test class with generic type parameters.
    /// </summary>
    /// <typeparam name="T">The type parameter</typeparam>
    public class GenericTypeTestSource<T>
    {
        public T Value { get; set; }
        public List<T> Items { get; set; }
    }

    /// <summary>
    /// Test class with multiple type parameters.
    /// </summary>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <typeparam name="TValue">The value type</typeparam>
    public class MultiGenericTypeTestSource<TKey, TValue>
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }
        public Dictionary<TKey, TValue> Items { get; set; }
    }
}
