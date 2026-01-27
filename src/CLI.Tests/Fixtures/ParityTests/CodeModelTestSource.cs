// Test fixture for code model completeness parity tests
// This file provides C# types for testing all code model properties

using System;
using System.Collections.Generic;

namespace Typewriter.CLI.Tests.Fixtures.ParityTests
{
    /// <summary>
    /// Interface for testing interface code model properties.
    /// </summary>
    public interface ICodeModelInterface
    {
        /// <summary>Interface property.</summary>
        string InterfaceProperty { get; set; }

        /// <summary>Interface method.</summary>
        void InterfaceMethod();

        /// <summary>Interface method with parameters.</summary>
        /// <param name="param1">First parameter.</param>
        /// <param name="param2">Second parameter.</param>
        /// <returns>A string result.</returns>
        string InterfaceMethodWithParams(string param1, int param2);
    }

    /// <summary>
    /// Generic interface for testing generic type parameters.
    /// </summary>
    /// <typeparam name="T">The type parameter.</typeparam>
    public interface IGenericInterface<T>
    {
        T GetValue();
        void SetValue(T value);
    }

    /// <summary>
    /// Abstract base class for testing inheritance and abstract members.
    /// </summary>
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
    /// This class tests: Name, FullName, Namespace, Attributes, Properties, Methods,
    /// Fields, Interfaces, BaseClass, TypeParameters, IsAbstract, IsStatic, etc.
    /// </summary>
    /// <typeparam name="TKey">Key type parameter.</typeparam>
    /// <typeparam name="TValue">Value type parameter.</typeparam>
    [Serializable]
    [Obsolete("Test attribute with message")]
    public class CodeModelTestClass<TKey, TValue> : CodeModelBaseClass, ICodeModelInterface, IGenericInterface<TValue>
        where TKey : class
        where TValue : struct
    {
        // ============================================
        // Fields
        // ============================================

        /// <summary>Public field.</summary>
        public string PublicField;

        /// <summary>Private field.</summary>
        private int _privateField;

        /// <summary>Protected field.</summary>
        protected bool ProtectedField;

        /// <summary>Readonly field.</summary>
        public readonly string ReadonlyField = "readonly";

        /// <summary>Static field.</summary>
        public static int StaticField;

        /// <summary>Const field.</summary>
        public const string ConstField = "const";

        // ============================================
        // Properties
        // ============================================

        /// <summary>
        /// Property with getter and setter.
        /// </summary>
        public string ReadWriteProperty { get; set; }

        /// <summary>
        /// Read-only property.
        /// </summary>
        public string ReadOnlyProperty { get; }

        /// <summary>
        /// Property with private setter.
        /// </summary>
        public string PrivateSetterProperty { get; private set; }

        /// <summary>
        /// Property with init setter (C# 9+).
        /// </summary>
        public string InitOnlyProperty { get; init; }

        /// <summary>
        /// Static property.
        /// </summary>
        public static string StaticProperty { get; set; }

        /// <summary>
        /// Property with default value.
        /// </summary>
        public string PropertyWithDefault { get; set; } = "default";

        /// <summary>
        /// Property with attribute.
        /// </summary>
        [Obsolete("Property is obsolete")]
        public string AttributedProperty { get; set; }

        /// <summary>
        /// Nullable property.
        /// </summary>
        public int? NullableProperty { get; set; }

        /// <summary>
        /// Interface property implementation.
        /// </summary>
        public string InterfaceProperty { get; set; }

        // ============================================
        // Methods
        // ============================================

        /// <summary>
        /// Simple method with no parameters.
        /// </summary>
        public void SimpleMethod() { }

        /// <summary>
        /// Method with parameters.
        /// </summary>
        /// <param name="param1">First string parameter.</param>
        /// <param name="param2">Second int parameter.</param>
        public void MethodWithParams(string param1, int param2) { }

        /// <summary>
        /// Method with return value.
        /// </summary>
        /// <returns>A string value.</returns>
        public string MethodWithReturn()
        {
            return "result";
        }

        /// <summary>
        /// Method with default parameter.
        /// </summary>
        /// <param name="value">Parameter with default value.</param>
        public void MethodWithDefault(string value = "default") { }

        /// <summary>
        /// Method with optional parameter.
        /// </summary>
        /// <param name="required">Required parameter.</param>
        /// <param name="optional">Optional parameter.</param>
        public void MethodWithOptional(string required, int optional = 0) { }

        /// <summary>
        /// Static method.
        /// </summary>
        public static void StaticMethod() { }

        /// <summary>
        /// Generic method.
        /// </summary>
        /// <typeparam name="T">Method type parameter.</typeparam>
        /// <param name="value">Value of type T.</param>
        /// <returns>The same value.</returns>
        public T GenericMethod<T>(T value) where T : class
        {
            return value;
        }

        /// <summary>
        /// Method with attribute.
        /// </summary>
        [Obsolete("Method is obsolete")]
        public void AttributedMethod() { }

        /// <summary>
        /// Async method.
        /// </summary>
        /// <returns>A task.</returns>
        public async System.Threading.Tasks.Task AsyncMethod()
        {
            await System.Threading.Tasks.Task.CompletedTask;
        }

        /// <summary>
        /// Async method with return value.
        /// </summary>
        /// <returns>A task containing a string.</returns>
        public async System.Threading.Tasks.Task<string> AsyncMethodWithReturn()
        {
            return await System.Threading.Tasks.Task.FromResult("result");
        }

        /// <summary>
        /// Abstract method implementation.
        /// </summary>
        public override void AbstractMethod() { }

        /// <summary>
        /// Virtual method override.
        /// </summary>
        public override void VirtualMethod() { }

        /// <summary>
        /// Interface method implementation.
        /// </summary>
        public void InterfaceMethod() { }

        /// <summary>
        /// Interface method with params implementation.
        /// </summary>
        public string InterfaceMethodWithParams(string param1, int param2)
        {
            return $"{param1}-{param2}";
        }

        /// <summary>
        /// Generic interface method implementation.
        /// </summary>
        public TValue GetValue() => default;

        /// <summary>
        /// Generic interface method implementation.
        /// </summary>
        public void SetValue(TValue value) { }

        // ============================================
        // Constructors
        // ============================================

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CodeModelTestClass() { }

        /// <summary>
        /// Constructor with parameters.
        /// </summary>
        /// <param name="readOnlyProperty">Value for read-only property.</param>
        public CodeModelTestClass(string readOnlyProperty)
        {
            ReadOnlyProperty = readOnlyProperty;
        }
    }

    /// <summary>
    /// Static class for testing static class code model properties.
    /// </summary>
    public static class StaticCodeModelClass
    {
        /// <summary>Static property.</summary>
        public static string StaticProperty { get; set; }

        /// <summary>Static method.</summary>
        public static void StaticMethod() { }

        /// <summary>Extension method.</summary>
        public static string ToCustomString(this string value)
        {
            return $"Custom: {value}";
        }
    }

    /// <summary>
    /// Enum for testing enum code model properties.
    /// </summary>
    public enum CodeModelEnum
    {
        /// <summary>First value.</summary>
        First = 0,

        /// <summary>Second value.</summary>
        Second = 1,

        /// <summary>Third value with explicit value.</summary>
        Third = 10,

        /// <summary>Fourth value.</summary>
        [Obsolete("Deprecated enum value")]
        Fourth = 20
    }

    /// <summary>
    /// Flags enum for testing flags enum code model.
    /// </summary>
    [Flags]
    public enum CodeModelFlagsEnum
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
        All = Read | Write | Execute
    }

    /// <summary>
    /// Record for testing record code model properties (C# 9+).
    /// </summary>
    /// <param name="Id">The identifier.</param>
    /// <param name="Name">The name.</param>
    public record CodeModelRecord(int Id, string Name)
    {
        /// <summary>Additional property.</summary>
        public string Description { get; init; }
    }

    /// <summary>
    /// Struct for testing struct code model properties.
    /// </summary>
    public struct CodeModelStruct
    {
        /// <summary>Struct property.</summary>
        public int Value { get; set; }

        /// <summary>Struct method.</summary>
        public void StructMethod() { }
    }

    /// <summary>
    /// Nested class container for testing nested type code model.
    /// </summary>
    public class OuterClass
    {
        /// <summary>Outer property.</summary>
        public string OuterProperty { get; set; }

        /// <summary>
        /// Nested class for testing nested type code model.
        /// </summary>
        public class NestedClass
        {
            /// <summary>Nested property.</summary>
            public string NestedProperty { get; set; }
        }

        /// <summary>
        /// Private nested class.
        /// </summary>
        private class PrivateNestedClass
        {
            public int Value { get; set; }
        }
    }
}
