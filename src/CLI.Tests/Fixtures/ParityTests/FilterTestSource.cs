// Test fixture for filter syntax parity tests
// This file provides C# types for testing CLI filter syntax against VS extension behavior

using System;

namespace Typewriter.CLI.Tests.Fixtures.ParityTests
{
    // ============================================
    // Name Pattern Filter Test Classes
    // ============================================

    /// <summary>Test class ending with "Model" for suffix filter testing.</summary>
    public class CustomerModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>Test class ending with "Model" for suffix filter testing.</summary>
    public class OrderModel
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
    }

    /// <summary>Test class ending with "Model" for suffix filter testing.</summary>
    public class ProductModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
    }

    /// <summary>Test class starting with "Base" for prefix filter testing.</summary>
    public class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Test class starting with "Base" for prefix filter testing.</summary>
    public class BaseController
    {
        public bool IsInitialized { get; set; }
    }

    /// <summary>Test class containing "Service" for middle wildcard filter testing.</summary>
    public interface ICustomerService
    {
        void Save();
    }

    /// <summary>Test class containing "Service" for middle wildcard filter testing.</summary>
    public class CustomerService : ICustomerService
    {
        public void Save() { }
    }

    /// <summary>Test class containing "Service" for middle wildcard filter testing.</summary>
    public class OrderService
    {
        public void Process() { }
    }

    /// <summary>Test class not matching common patterns.</summary>
    public class Helper
    {
        public void DoWork() { }
    }

    // ============================================
    // Attribute Filter Test Classes
    // ============================================

    /// <summary>Custom attribute for testing attribute filters.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method)]
    public class CustomFilterAttribute : Attribute
    {
        public string Value { get; set; }

        public CustomFilterAttribute() { }
        public CustomFilterAttribute(string value) { Value = value; }
    }

    /// <summary>Another custom attribute for testing multiple attributes.</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MarkerAttribute : Attribute { }

    /// <summary>Test class with Serializable attribute.</summary>
    [Serializable]
    public class SerializableEntity
    {
        public int Id { get; set; }
        public string Data { get; set; }
    }

    /// <summary>Test class with custom attribute.</summary>
    [CustomFilter("test-value")]
    public class CustomFilteredClass
    {
        public int Id { get; set; }
    }

    /// <summary>Test class with marker attribute.</summary>
    [Marker]
    public class MarkedClass
    {
        public string Name { get; set; }
    }

    /// <summary>Test class with multiple attributes.</summary>
    [Serializable]
    [CustomFilter]
    [Marker]
    public class MultiAttributeClass
    {
        public int Id { get; set; }
    }

    /// <summary>Test class without any attributes.</summary>
    public class UnattributedClass
    {
        public int Id { get; set; }
    }

    // ============================================
    // Inheritance Filter Test Classes
    // ============================================

    /// <summary>Base class for inheritance filter testing.</summary>
    public abstract class FilterBaseClass
    {
        public int BaseId { get; set; }
    }

    /// <summary>Interface for inheritance filter testing.</summary>
    public interface IFilterInterface
    {
        void Execute();
    }

    /// <summary>Another interface for multiple inheritance testing.</summary>
    public interface IAnotherInterface
    {
        string Name { get; }
    }

    /// <summary>Derived class for inheritance filter testing.</summary>
    public class DerivedFromBase : FilterBaseClass
    {
        public string DerivedProperty { get; set; }
    }

    /// <summary>Another derived class for inheritance filter testing.</summary>
    public class AnotherDerivedFromBase : FilterBaseClass
    {
        public int AnotherProperty { get; set; }
    }

    /// <summary>Class implementing interface for inheritance filter testing.</summary>
    public class ImplementsInterface : IFilterInterface
    {
        public void Execute() { }
    }

    /// <summary>Class implementing multiple interfaces.</summary>
    public class ImplementsMultipleInterfaces : IFilterInterface, IAnotherInterface
    {
        public string Name => "Test";
        public void Execute() { }
    }

    /// <summary>Class both inheriting and implementing interface.</summary>
    public class DerivedAndImplementing : FilterBaseClass, IFilterInterface
    {
        public void Execute() { }
    }

    /// <summary>Standalone class not inheriting from test base.</summary>
    public class StandaloneClass
    {
        public int Id { get; set; }
    }

    // ============================================
    // Predicate Filter Test Classes
    // ============================================

    /// <summary>Test class with properties for predicate filtering.</summary>
    public class PredicateTestClass
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>Test class that should be included by predicate.</summary>
    public class IncludedByPredicate
    {
        public int Value { get; set; }
    }

    /// <summary>Test class that should be excluded by predicate.</summary>
    public class ExcludedByPredicate
    {
        public int Value { get; set; }
    }
}
