using NSubstitute;
using Shouldly;
using Typewriter.CLI.Generation;
using Typewriter.CodeModel;
using Typewriter.Core.Abstractions;
using Xunit;
using Type = System.Type;

namespace Typewriter.CLI.Tests;

/// <summary>
/// Tests that verify CLI template rendering produces output matching expected patterns.
/// These tests ensure parity with the VS extension's template engine.
/// </summary>
public class TemplateParityTests
{
    #region Test Infrastructure

    private IErrorReporter CreateMockErrorReporter()
    {
        return Substitute.For<IErrorReporter>();
    }

    /// <summary>
    /// Mock class implementation for testing.
    /// </summary>
    private class MockClass : Item
    {
        public string Name { get; set; } = string.Empty;
        public string name => char.ToLowerInvariant(Name[0]) + Name.Substring(1);
        public string FullName { get; set; } = string.Empty;
        public bool IsAbstract { get; set; }
        public bool IsGeneric { get; set; }
        public MockPropertyCollection Properties { get; set; } = new();
        public MockMethodCollection Methods { get; set; } = new();
    }

    /// <summary>
    /// Mock property implementation for testing.
    /// </summary>
    private class MockProperty : Item
    {
        public string Name { get; set; } = string.Empty;
        public string name => char.ToLowerInvariant(Name[0]) + Name.Substring(1);
        public string Type { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public bool HasGetter { get; set; } = true;
        public bool HasSetter { get; set; } = true;
    }

    /// <summary>
    /// Mock method implementation for testing.
    /// </summary>
    private class MockMethod : Item
    {
        public string Name { get; set; } = string.Empty;
        public string name => char.ToLowerInvariant(Name[0]) + Name.Substring(1);
        public string Type { get; set; } = "void";
        public MockParameterCollection Parameters { get; set; } = new();
    }

    /// <summary>
    /// Mock parameter implementation for testing.
    /// </summary>
    private class MockParameter : Item
    {
        public string Name { get; set; } = string.Empty;
        public string name => char.ToLowerInvariant(Name[0]) + Name.Substring(1);
        public string Type { get; set; } = string.Empty;
    }

    /// <summary>
    /// Mock file implementation for testing.
    /// </summary>
    private class MockFile : Item
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public MockClassCollection Classes { get; set; } = new();
        public MockInterfaceCollection Interfaces { get; set; } = new();
        public MockEnumCollection Enums { get; set; } = new();
    }

    /// <summary>
    /// Mock enum implementation for testing.
    /// </summary>
    private class MockEnum : Item
    {
        public string Name { get; set; } = string.Empty;
        public string name => char.ToLowerInvariant(Name[0]) + Name.Substring(1);
        public MockEnumValueCollection Values { get; set; } = new();
    }

    /// <summary>
    /// Mock enum value implementation for testing.
    /// </summary>
    private class MockEnumValue : Item
    {
        public string Name { get; set; } = string.Empty;
        public string name => char.ToLowerInvariant(Name[0]) + Name.Substring(1);
        public long Value { get; set; }
    }

    /// <summary>
    /// Mock interface implementation for testing.
    /// </summary>
    private class MockInterface : Item
    {
        public string Name { get; set; } = string.Empty;
        public string name => char.ToLowerInvariant(Name[0]) + Name.Substring(1);
        public MockPropertyCollection Properties { get; set; } = new();
        public MockMethodCollection Methods { get; set; } = new();
    }

    #region Mock Collections

    private class MockClassCollection : List<Item>, IFilterable
    {
        public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
            item => item is MockClass c ? Array.Empty<string>() : Array.Empty<string>();

        public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> ItemFilterSelector =>
            item => item is MockClass c ? new[] { c.Name } : Array.Empty<string>();
    }

    private class MockPropertyCollection : List<Item>, IFilterable
    {
        public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> ItemFilterSelector =>
            item => item is MockProperty p ? new[] { p.Name } : Array.Empty<string>();
    }

    private class MockMethodCollection : List<Item>, IFilterable
    {
        public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> ItemFilterSelector =>
            item => item is MockMethod m ? new[] { m.Name } : Array.Empty<string>();
    }

    private class MockParameterCollection : List<Item>, IFilterable
    {
        public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> ItemFilterSelector =>
            item => item is MockParameter p ? new[] { p.Name } : Array.Empty<string>();
    }

    private class MockInterfaceCollection : List<Item>, IFilterable
    {
        public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> ItemFilterSelector =>
            item => item is MockInterface i ? new[] { i.Name } : Array.Empty<string>();
    }

    private class MockEnumCollection : List<Item>, IFilterable
    {
        public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> ItemFilterSelector =>
            item => item is MockEnum e ? new[] { e.Name } : Array.Empty<string>();
    }

    private class MockEnumValueCollection : List<Item>, IFilterable
    {
        public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
            item => Array.Empty<string>();

        public Func<Item, IEnumerable<string>> ItemFilterSelector =>
            item => item is MockEnumValue v ? new[] { v.Name } : Array.Empty<string>();
    }

    #endregion

    /// <summary>
    /// Creates a mock file with a simple model class.
    /// </summary>
    private static MockFile CreateSimpleModelFile()
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
    /// Creates a mock file with complex model containing nullable types and nested class.
    /// </summary>
    private static MockFile CreateComplexModelFile()
    {
        var file = new MockFile
        {
            Name = "ComplexModel.cs",
            FullName = @"C:\Test\Models\ComplexModel.cs"
        };

        var complexModel = new MockClass
        {
            Name = "ComplexModel",
            FullName = "TestFixtures.Models.ComplexModel"
        };

        complexModel.Properties.Add(new MockProperty { Name = "Id", Type = "number" });
        complexModel.Properties.Add(new MockProperty { Name = "Name", Type = "string" });
        complexModel.Properties.Add(new MockProperty { Name = "Description", Type = "string", IsNullable = true });
        complexModel.Properties.Add(new MockProperty { Name = "CreatedAt", Type = "Date" });
        complexModel.Properties.Add(new MockProperty { Name = "ModifiedAt", Type = "Date", IsNullable = true });
        complexModel.Properties.Add(new MockProperty { Name = "Status", Type = "ModelStatus" });
        complexModel.Properties.Add(new MockProperty { Name = "Price", Type = "number" });
        complexModel.Properties.Add(new MockProperty { Name = "Tags", Type = "string[]" });

        file.Classes.Add(complexModel);

        return file;
    }

    /// <summary>
    /// Creates a mock file with an enum.
    /// </summary>
    private static MockFile CreateEnumFile()
    {
        var file = new MockFile
        {
            Name = "ModelStatus.cs",
            FullName = @"C:\Test\Models\ModelStatus.cs"
        };

        var statusEnum = new MockEnum { Name = "ModelStatus" };
        statusEnum.Values.Add(new MockEnumValue { Name = "Draft", Value = 0 });
        statusEnum.Values.Add(new MockEnumValue { Name = "Active", Value = 1 });
        statusEnum.Values.Add(new MockEnumValue { Name = "Archived", Value = 2 });

        file.Enums.Add(statusEnum);

        return file;
    }

    /// <summary>
    /// Creates a mock file with interfaces.
    /// </summary>
    private static MockFile CreateInterfaceFile()
    {
        var file = new MockFile
        {
            Name = "Interfaces.cs",
            FullName = @"C:\Test\Models\Interfaces.cs"
        };

        var entityInterface = new MockInterface { Name = "IEntity" };
        entityInterface.Properties.Add(new MockProperty { Name = "Id", Type = "number" });

        var auditableInterface = new MockInterface { Name = "IAuditable" };
        auditableInterface.Properties.Add(new MockProperty { Name = "CreatedAt", Type = "Date" });
        auditableInterface.Properties.Add(new MockProperty { Name = "CreatedBy", Type = "string" });

        file.Interfaces.Add(entityInterface);
        file.Interfaces.Add(auditableInterface);

        return file;
    }

    #endregion

    #region Basic Interface Generation Tests

    [Fact]
    public void Render_SimpleClass_GeneratesTypeScriptInterface()
    {
        var errorReporter = CreateMockErrorReporter();
        var file = CreateSimpleModelFile();

        const string template = @"$Classes[
export interface $Name {
    $Properties[$name: $Type;
    ]
}
]";

        var result = CliParser.Parse(
            "test.tst",
            file.FullName,
            template,
            new List<Type>(),
            file,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.ShouldContain("export interface SimpleModel");
        result.ShouldContain("id: number;");
        result.ShouldContain("name: string;");
        result.ShouldContain("isActive: boolean;");
    }

    [Fact]
    public void Render_WithSeparators_JoinsPropertiesCorrectly()
    {
        var errorReporter = CreateMockErrorReporter();
        var file = CreateSimpleModelFile();

        const string template = @"$Classes[
export interface $Name {
    $Properties[$name: $Type][;
    ];
}
]";

        var result = CliParser.Parse(
            "test.tst",
            file.FullName,
            template,
            new List<Type>(),
            file,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldNotBeNull();
        // Verify properties are separated correctly
        result.ShouldContain("id: number;");
        result.ShouldContain("name: string;");
        result.ShouldContain("isActive: boolean;");
    }

    #endregion

    #region Filter Pattern Tests

    [Fact]
    public void Render_WithNameFilter_FiltersClasses()
    {
        var errorReporter = CreateMockErrorReporter();
        var file = new MockFile
        {
            Name = "Models.cs",
            FullName = @"C:\Test\Models.cs"
        };

        file.Classes.Add(new MockClass { Name = "CustomerModel" });
        file.Classes.Add(new MockClass { Name = "OrderModel" });
        file.Classes.Add(new MockClass { Name = "Product" });

        const string template = @"$Classes(*Model)[
export interface $Name {}
]";

        var result = CliParser.Parse(
            "test.tst",
            file.FullName,
            template,
            new List<Type>(),
            file,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.ShouldContain("CustomerModel");
        result.ShouldContain("OrderModel");
        result.ShouldNotContain("Product");
    }

    [Fact]
    public void Render_WithPrefixFilter_FiltersCorrectly()
    {
        var errorReporter = CreateMockErrorReporter();
        var file = new MockFile
        {
            Name = "Models.cs",
            FullName = @"C:\Test\Models.cs"
        };

        file.Classes.Add(new MockClass { Name = "IEntity" });
        file.Classes.Add(new MockClass { Name = "IRepository" });
        file.Classes.Add(new MockClass { Name = "Entity" });

        const string template = @"$Classes(I*)[
export interface $Name {}
]";

        var result = CliParser.Parse(
            "test.tst",
            file.FullName,
            template,
            new List<Type>(),
            file,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.ShouldContain("IEntity");
        result.ShouldContain("IRepository");
        result.ShouldNotContain("export interface Entity");
    }

    #endregion

    #region Enum Generation Tests

    [Fact]
    public void Render_Enum_GeneratesTypeScriptEnum()
    {
        var errorReporter = CreateMockErrorReporter();
        var file = CreateEnumFile();

        const string template = @"$Enums[
export enum $Name {
    $Values[$Name = $Value][,
    ]
}
]";

        var result = CliParser.Parse(
            "test.tst",
            file.FullName,
            template,
            new List<Type>(),
            file,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.ShouldContain("export enum ModelStatus");
        result.ShouldContain("Draft = 0");
        result.ShouldContain("Active = 1");
        result.ShouldContain("Archived = 2");
    }

    #endregion

    #region Interface Generation Tests

    [Fact]
    public void Render_Interfaces_GeneratesTypeScriptInterfaces()
    {
        var errorReporter = CreateMockErrorReporter();
        var file = CreateInterfaceFile();

        const string template = @"$Interfaces[
export interface $Name {
    $Properties[$name: $Type;
    ]
}
]";

        var result = CliParser.Parse(
            "test.tst",
            file.FullName,
            template,
            new List<Type>(),
            file,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.ShouldContain("export interface IEntity");
        result.ShouldContain("id: number;");
        result.ShouldContain("export interface IAuditable");
        result.ShouldContain("createdAt: Date;");
        result.ShouldContain("createdBy: string;");
    }

    #endregion

    #region Boolean Conditional Tests

    [Fact]
    public void Render_BooleanTrue_SelectsTrueBlock()
    {
        var errorReporter = CreateMockErrorReporter();
        var file = new MockFile { Name = "Test.cs", FullName = @"C:\Test.cs" };

        var abstractClass = new MockClass { Name = "AbstractEntity", IsAbstract = true };
        file.Classes.Add(abstractClass);

        const string template = @"$Classes[
export $IsAbstract[abstract ][concrete ]class $Name {}
]";

        var result = CliParser.Parse(
            "test.tst",
            file.FullName,
            template,
            new List<Type>(),
            file,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.ShouldContain("export abstract class AbstractEntity");
    }

    [Fact]
    public void Render_BooleanFalse_SelectsFalseBlock()
    {
        var errorReporter = CreateMockErrorReporter();
        var file = new MockFile { Name = "Test.cs", FullName = @"C:\Test.cs" };

        var concreteClass = new MockClass { Name = "ConcreteEntity", IsAbstract = false };
        file.Classes.Add(concreteClass);

        const string template = @"$Classes[
export $IsAbstract[abstract ][concrete ]class $Name {}
]";

        var result = CliParser.Parse(
            "test.tst",
            file.FullName,
            template,
            new List<Type>(),
            file,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.ShouldContain("export concrete class ConcreteEntity");
    }

    #endregion

    #region Nested Collection Tests

    [Fact]
    public void Render_MethodWithParameters_GeneratesCorrectSignature()
    {
        var errorReporter = CreateMockErrorReporter();
        var file = new MockFile { Name = "Service.cs", FullName = @"C:\Test\Service.cs" };

        var serviceClass = new MockClass { Name = "UserService" };

        var getMethod = new MockMethod { Name = "GetUser", Type = "User" };
        getMethod.Parameters.Add(new MockParameter { Name = "Id", Type = "number" });

        var createMethod = new MockMethod { Name = "CreateUser", Type = "User" };
        createMethod.Parameters.Add(new MockParameter { Name = "Name", Type = "string" });
        createMethod.Parameters.Add(new MockParameter { Name = "Email", Type = "string" });

        serviceClass.Methods.Add(getMethod);
        serviceClass.Methods.Add(createMethod);
        file.Classes.Add(serviceClass);

        const string template = @"$Classes[
export class $Name {
    $Methods[
    $name($Parameters[$name: $Type][, ]): $Type;]
}
]";

        var result = CliParser.Parse(
            "test.tst",
            file.FullName,
            template,
            new List<Type>(),
            file,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.ShouldContain("export class UserService");
        result.ShouldContain("getUser(id: number): User;");
        result.ShouldContain("createUser(name: string, email: string): User;");
    }

    #endregion

    #region Complex Model Tests

    [Fact]
    public void Render_ComplexModel_GeneratesAllProperties()
    {
        var errorReporter = CreateMockErrorReporter();
        var file = CreateComplexModelFile();

        const string template = @"$Classes[
export interface $Name {
    $Properties[$name: $Type;
    ]
}
]";

        var result = CliParser.Parse(
            "test.tst",
            file.FullName,
            template,
            new List<Type>(),
            file,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.ShouldContain("export interface ComplexModel");
        result.ShouldContain("id: number;");
        result.ShouldContain("name: string;");
        result.ShouldContain("description: string;");
        result.ShouldContain("createdAt: Date;");
        result.ShouldContain("modifiedAt: Date;");
        result.ShouldContain("status: ModelStatus;");
        result.ShouldContain("price: number;");
        result.ShouldContain("tags: string[];");
    }

    #endregion

    #region Multiple Classes in Single Template Tests

    [Fact]
    public void Render_MultipleClasses_GeneratesAllInterfaces()
    {
        var errorReporter = CreateMockErrorReporter();
        var file = new MockFile { Name = "Models.cs", FullName = @"C:\Test\Models.cs" };

        var customer = new MockClass { Name = "Customer" };
        customer.Properties.Add(new MockProperty { Name = "Id", Type = "number" });
        customer.Properties.Add(new MockProperty { Name = "Name", Type = "string" });

        var order = new MockClass { Name = "Order" };
        order.Properties.Add(new MockProperty { Name = "Id", Type = "number" });
        order.Properties.Add(new MockProperty { Name = "Total", Type = "number" });

        file.Classes.Add(customer);
        file.Classes.Add(order);

        const string template = @"$Classes[
export interface $Name {
    $Properties[$name: $Type;
    ]
}
]";

        var result = CliParser.Parse(
            "test.tst",
            file.FullName,
            template,
            new List<Type>(),
            file,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.ShouldContain("export interface Customer");
        result.ShouldContain("export interface Order");
        result.ShouldContain("id: number;");
        result.ShouldContain("name: string;");
        result.ShouldContain("total: number;");
    }

    #endregion
}
