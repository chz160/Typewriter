using NSubstitute;
using Shouldly;
using Typewriter.CLI.Generation;
using Typewriter.CodeModel;
using Typewriter.Core.Abstractions;
using Xunit;
using Type = System.Type;

namespace Typewriter.CLI.Tests;

#region Test Mock Classes

/// <summary>
/// A test item that can be used in template parsing tests.
/// </summary>
public class TestItem : Item
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
}

/// <summary>
/// A test collection of items that implements IFilterable.
/// </summary>
public class TestItemCollection : List<Item>, IFilterable
{
    public Func<Item, IEnumerable<string>> AttributeFilterSelector =>
        item => item is TestAttributeItem ai ? ai.Attributes : Array.Empty<string>();

    public Func<Item, IEnumerable<string>> InheritanceFilterSelector =>
        item => item is TestAttributeItem ai ? ai.BaseTypes : Array.Empty<string>();

    public Func<Item, IEnumerable<string>> ItemFilterSelector =>
        item => item is TestAttributeItem ai ? new[] { ai.Name } : Array.Empty<string>();
}

/// <summary>
/// A test item with attributes and base types for filter testing.
/// </summary>
public class TestAttributeItem : Item
{
    public string Name { get; set; } = string.Empty;
    public List<string> Attributes { get; set; } = new();
    public List<string> BaseTypes { get; set; } = new();
}

/// <summary>
/// A test context object for parser tests.
/// </summary>
public class TestContext
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsClass { get; set; }
    public int Count { get; set; }
    public TestItemCollection Items { get; set; } = new();
}

#endregion

#region TemplateStream Tests

public class TemplateStreamTests
{
    [Fact]
    public void Advance_MovesToNextCharacter()
    {
        var stream = new TemplateStream("abc");

        stream.Advance().ShouldBeTrue();
        stream.Current.ShouldBe('a');

        stream.Advance().ShouldBeTrue();
        stream.Current.ShouldBe('b');

        stream.Advance().ShouldBeTrue();
        stream.Current.ShouldBe('c');

        stream.Advance().ShouldBeFalse();
    }

    [Fact]
    public void Advance_WithOffset_SkipsMultipleCharacters()
    {
        var stream = new TemplateStream("abcdef");

        stream.Advance(3).ShouldBeTrue();
        stream.Current.ShouldBe('c');
    }

    [Fact]
    public void Peek_ReturnsCharacterAtOffset()
    {
        var stream = new TemplateStream("abcdef");
        stream.Advance();

        stream.Peek(0).ShouldBe('a');
        stream.Peek(1).ShouldBe('b');
        stream.Peek(2).ShouldBe('c');
    }

    [Fact]
    public void Peek_OutOfBounds_ReturnsMinValue()
    {
        var stream = new TemplateStream("ab");
        stream.Advance();

        stream.Peek(10).ShouldBe(char.MinValue);
        stream.Peek(-10).ShouldBe(char.MinValue);
    }

    [Fact]
    public void PeekWord_ReturnsIdentifier()
    {
        var stream = new TemplateStream("$Name test");
        stream.Advance(); // Move to $

        stream.PeekWord(1).ShouldBe("Name");
    }

    [Fact]
    public void PeekWord_ReturnsNullForNonLetter()
    {
        var stream = new TemplateStream("123abc");
        stream.Advance();

        stream.PeekWord(0).ShouldBeNull();
    }

    [Fact]
    public void PeekWord_IncludesDigitsAfterLetter()
    {
        var stream = new TemplateStream("Class2Name");
        stream.Advance();

        stream.PeekWord(0).ShouldBe("Class2Name");
    }

    [Fact]
    public void PeekLine_ReturnsLineIncludingNewline()
    {
        var stream = new TemplateStream("line1\nline2");
        stream.Advance();

        stream.PeekLine(0).ShouldBe("line1\n");
    }

    [Fact]
    public void PeekBlock_ReturnsContentBetweenBrackets()
    {
        // PeekBlock starts at offset and reads until matching close bracket
        // It does NOT include the opening bracket when starting after it
        var stream = new TemplateStream("$Name[template]rest");
        stream.Advance(5); // Move to 'e' after 'Name'
        // Peek(1) = '[', so PeekBlock(2, ...) starts after the '['
        stream.PeekBlock(2, '[', ']').ShouldBe("template");
    }

    [Fact]
    public void PeekBlock_HandlesNestedBrackets()
    {
        var stream = new TemplateStream("$Items[outer[inner]more]rest");
        stream.Advance(6); // Move to 's' after 'Items'
        // Peek(1) = '[', so PeekBlock(2, ...) starts after the '['
        stream.PeekBlock(2, '[', ']').ShouldBe("outer[inner]more");
    }

    [Fact]
    public void PeekBlock_HandlesParentheses()
    {
        var stream = new TemplateStream("$Classes(*Model)");
        stream.Advance(8); // Move to 's' after 'Classes'
        // Peek(1) = '(', so PeekBlock(2, ...) starts after the '('
        stream.PeekBlock(2, '(', ')').ShouldBe("*Model");
    }

    [Fact]
    public void SkipWhitespace_AdvancesPastSpaces()
    {
        var stream = new TemplateStream("   abc");

        stream.SkipWhitespace();
        stream.Current.ShouldBe('a');
    }

    [Fact]
    public void Position_TracksCurrentPosition()
    {
        var stream = new TemplateStream("abcdef", 10);

        stream.Advance();
        stream.Position.ShouldBe(10);

        stream.Advance(3);
        stream.Position.ShouldBe(13);
    }

    [Fact]
    public void EmptyString_AdvanceReturnsFalse()
    {
        var stream = new TemplateStream("");

        stream.Advance().ShouldBeFalse();
        stream.Current.ShouldBe(char.MinValue);
    }
}

#endregion

#region CliItemFilter Tests

public class CliItemFilterTests
{
    [Fact]
    public void Apply_EmptyFilter_ReturnsAllItems()
    {
        var collection = CreateTestCollection("CustomerModel", "OrderModel", "Product");
        var matchFound = false;

        var result = CliItemFilter.Apply(collection, null, ref matchFound);

        result.Count().ShouldBe(3);
    }

    [Fact]
    public void Apply_WhitespaceFilter_ReturnsAllItems()
    {
        var collection = CreateTestCollection("CustomerModel", "OrderModel", "Product");
        var matchFound = false;

        var result = CliItemFilter.Apply(collection, "   ", ref matchFound);

        result.Count().ShouldBe(3);
    }

    [Fact]
    public void Apply_ExactMatchFilter_ReturnsMatchingItems()
    {
        var collection = CreateTestCollection("Customer", "Order", "Product");
        var matchFound = false;

        var result = CliItemFilter.Apply(collection, "Order", ref matchFound).ToList();

        result.Count.ShouldBe(1);
        ((TestAttributeItem)result[0]).Name.ShouldBe("Order");
        matchFound.ShouldBeTrue();
    }

    [Fact]
    public void Apply_WildcardSuffixFilter_ReturnsMatchingItems()
    {
        var collection = CreateTestCollection("CustomerModel", "OrderModel", "Product");
        var matchFound = false;

        var result = CliItemFilter.Apply(collection, "*Model", ref matchFound).ToList();

        result.Count.ShouldBe(2);
        matchFound.ShouldBeTrue();
    }

    [Fact]
    public void Apply_WildcardPrefixFilter_ReturnsMatchingItems()
    {
        var collection = CreateTestCollection("ICustomer", "IOrder", "Product");
        var matchFound = false;

        var result = CliItemFilter.Apply(collection, "I*", ref matchFound).ToList();

        result.Count.ShouldBe(2);
        matchFound.ShouldBeTrue();
    }

    [Fact]
    public void Apply_WildcardContainsFilter_ReturnsMatchingItems()
    {
        var collection = CreateTestCollection("CustomerDto", "OrderDto", "Product");
        var matchFound = false;

        var result = CliItemFilter.Apply(collection, "*Dto*", ref matchFound).ToList();

        // *Dto* ends with wildcard so matches CustomerDto and OrderDto
        result.Count.ShouldBe(2);
    }

    [Fact]
    public void Apply_AttributeFilter_ReturnsItemsWithAttribute()
    {
        var collection = new TestItemCollection
        {
            new TestAttributeItem { Name = "Customer", Attributes = new List<string> { "Serializable", "DataContract" } },
            new TestAttributeItem { Name = "Order", Attributes = new List<string> { "Serializable" } },
            new TestAttributeItem { Name = "Product", Attributes = new List<string>() }
        };
        var matchFound = false;

        var result = CliItemFilter.Apply(collection, "[Serializable]", ref matchFound).ToList();

        result.Count.ShouldBe(2);
        matchFound.ShouldBeTrue();
    }

    [Fact]
    public void Apply_InheritanceFilter_ReturnsItemsWithBaseType()
    {
        var collection = new TestItemCollection
        {
            new TestAttributeItem { Name = "Customer", BaseTypes = new List<string> { "Entity", "INotifyPropertyChanged" } },
            new TestAttributeItem { Name = "Order", BaseTypes = new List<string> { "Entity" } },
            new TestAttributeItem { Name = "Product", BaseTypes = new List<string> { "ValueObject" } }
        };
        var matchFound = false;

        var result = CliItemFilter.Apply(collection, ":Entity", ref matchFound).ToList();

        result.Count.ShouldBe(2);
        matchFound.ShouldBeTrue();
    }

    [Fact]
    public void Apply_NoMatch_SetsMatchFoundFalse()
    {
        var collection = CreateTestCollection("Customer", "Order", "Product");
        var matchFound = false;

        var result = CliItemFilter.Apply(collection, "NonExistent", ref matchFound).ToList();

        result.Count.ShouldBe(0);
        matchFound.ShouldBeFalse();
    }

    [Fact]
    public void Apply_NonFilterableCollection_ReturnsOriginal()
    {
        var items = new List<Item>
        {
            new TestItem { Name = "Test1" },
            new TestItem { Name = "Test2" }
        };
        var matchFound = false;

        // Non-IFilterable collection should be returned as-is
        var result = CliItemFilter.Apply(items, "*Test*", ref matchFound);

        result.Count().ShouldBe(2);
    }

    [Fact]
    public void Apply_CaseInsensitive_MatchesRegardlessOfCase()
    {
        var collection = CreateTestCollection("CustomerModel", "customermodel", "CUSTOMERMODEL");
        var matchFound = false;

        var result = CliItemFilter.Apply(collection, "customermodel", ref matchFound).ToList();

        result.Count.ShouldBe(3); // All should match due to case-insensitivity
    }

    private static TestItemCollection CreateTestCollection(params string[] names)
    {
        var collection = new TestItemCollection();
        foreach (var name in names)
        {
            collection.Add(new TestAttributeItem { Name = name });
        }
        return collection;
    }
}

#endregion

#region CliParser Tests

public class CliParserTests
{
    private IErrorReporter CreateMockErrorReporter()
    {
        return Substitute.For<IErrorReporter>();
    }

    /// <summary>
    /// Creates a test context with items so _matchFound is set to true.
    /// The parser requires at least one collection match to return output.
    /// </summary>
    private static TestContext CreateContextWithItems(string name = "Customer")
    {
        var context = new TestContext { Name = name };
        context.Items.Add(new TestAttributeItem { Name = "Item1" });
        return context;
    }

    [Fact]
    public void Parse_CollectionWithItems_SetsMatchFoundTrue()
    {
        var errorReporter = CreateMockErrorReporter();
        var context = CreateContextWithItems("Customer");

        var result = CliParser.Parse(
            "test.tst",
            "source.cs",
            "$Items[$Name]",
            new List<Type>(),
            null, // templateInstance
            context,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldBe("Item1");
    }

    [Fact]
    public void Parse_CollectionWithFilter_FiltersItems()
    {
        var errorReporter = CreateMockErrorReporter();
        var context = new TestContext { Name = "Customer" };
        context.Items.Add(new TestAttributeItem { Name = "CustomerModel" });
        context.Items.Add(new TestAttributeItem { Name = "OrderModel" });
        context.Items.Add(new TestAttributeItem { Name = "Product" });

        var result = CliParser.Parse(
            "test.tst",
            "source.cs",
            "$Items(*Model)[$Name][, ]",
            new List<Type>(),
            null, // templateInstance
            context,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldBe("CustomerModel, OrderModel");
    }

    [Fact]
    public void Parse_CollectionWithSeparator_JoinsWithSeparator()
    {
        var errorReporter = CreateMockErrorReporter();
        var context = new TestContext { Name = "Customer" };
        context.Items.Add(new TestAttributeItem { Name = "A" });
        context.Items.Add(new TestAttributeItem { Name = "B" });
        context.Items.Add(new TestAttributeItem { Name = "C" });

        var result = CliParser.Parse(
            "test.tst",
            "source.cs",
            "$Items[$Name][ | ]",
            new List<Type>(),
            null, // templateInstance
            context,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldBe("A | B | C");
    }

    [Fact]
    public void Parse_EmptyCollection_ReturnsNull()
    {
        var errorReporter = CreateMockErrorReporter();
        var context = new TestContext { Name = "Customer" };
        // Empty Items collection

        var result = CliParser.Parse(
            "test.tst",
            "source.cs",
            "$Items[$Name]",
            new List<Type>(),
            null, // templateInstance
            context,
            errorReporter,
            out var success);

        // No match found with empty collection
        result.ShouldBeNull();
    }

    [Fact]
    public void Parse_BooleanIdentifier_TrueBlock()
    {
        var errorReporter = CreateMockErrorReporter();
        var context = CreateContextWithItems("Customer");
        context.IsClass = true;

        var result = CliParser.Parse(
            "test.tst",
            "source.cs",
            "$Items[$Name] $IsClass[class][interface]",
            new List<Type>(),
            null, // templateInstance
            context,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldBe("Item1 class");
    }

    [Fact]
    public void Parse_BooleanIdentifier_FalseBlock()
    {
        var errorReporter = CreateMockErrorReporter();
        var context = CreateContextWithItems("Customer");
        context.IsClass = false;

        var result = CliParser.Parse(
            "test.tst",
            "source.cs",
            "$Items[$Name] $IsClass[class][interface]",
            new List<Type>(),
            null, // templateInstance
            context,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldBe("Item1 interface");
    }

    [Fact]
    public void Parse_EmptyTemplate_ReturnsNull()
    {
        var errorReporter = CreateMockErrorReporter();
        var context = new TestContext();

        var result = CliParser.Parse(
            "test.tst",
            "source.cs",
            "",
            new List<Type>(),
            null, // templateInstance
            context,
            errorReporter,
            out var success);

        result.ShouldBeNull();
    }

    [Fact]
    public void Parse_NullTemplate_ReturnsNull()
    {
        var errorReporter = CreateMockErrorReporter();
        var context = new TestContext();

        var result = CliParser.Parse(
            "test.tst",
            "source.cs",
            null!,
            new List<Type>(),
            null, // templateInstance
            context,
            errorReporter,
            out var success);

        result.ShouldBeNull();
    }

    [Fact]
    public void Parse_IdentifierWithBlockAfterCollection_AppliesBlock()
    {
        var errorReporter = CreateMockErrorReporter();
        var context = CreateContextWithItems("Customer");

        // When using $Name[block], the block is rendered with the Name value as context
        // So $Name resolves to "Customer" string, and the block "_$Name" has no accessible properties
        var result = CliParser.Parse(
            "test.tst",
            "source.cs",
            "$Items[$Name] I$Name",
            new List<Type>(),
            null, // templateInstance
            context,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldBe("Item1 ICustomer");
    }

    [Fact]
    public void Parse_NumericIdentifierAfterCollection_ReturnsValue()
    {
        var errorReporter = CreateMockErrorReporter();
        var context = CreateContextWithItems("Test");
        context.Count = 42;

        var result = CliParser.Parse(
            "test.tst",
            "source.cs",
            "$Items[$Name] Count: $Count",
            new List<Type>(),
            null, // templateInstance
            context,
            errorReporter,
            out var success);

        success.ShouldBeTrue();
        result.ShouldBe("Item1 Count: 42");
    }
}

#endregion

#region CliTemplateCodeParser Tests

public class CliTemplateCodeParserTests
{
    private IErrorReporter CreateMockErrorReporter()
    {
        return Substitute.For<IErrorReporter>();
    }

    [Fact]
    public void Parse_EmptyTemplate_ReturnsEmptyString()
    {
        var errorReporter = CreateMockErrorReporter();
        var extensions = new List<Type>();

        var result = CliTemplateCodeParser.Parse(
            "test.tst",
            "",
            extensions,
            errorReporter);

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Parse_NoCodeBlocks_ReturnsTemplateAsIs()
    {
        var errorReporter = CreateMockErrorReporter();
        var extensions = new List<Type>();

        var result = CliTemplateCodeParser.Parse(
            "test.tst",
            "$Classes[$Name]",
            extensions,
            errorReporter);

        result.ShouldBe("$Classes[$Name]");
    }

    [Fact]
    public void Parse_SimpleCodeBlock_RemovesCodeBlock()
    {
        var errorReporter = CreateMockErrorReporter();
        var extensions = new List<Type>();

        var result = CliTemplateCodeParser.Parse(
            "test.tst",
            "${using System;}\n$Classes[$Name]",
            extensions,
            errorReporter);

        // Code block should be removed from output
        result.ShouldBe("\n$Classes[$Name]");
    }

    [Fact]
    public void Parse_CommentedCodeBlock_PreservesCommentedBlock()
    {
        var errorReporter = CreateMockErrorReporter();
        var extensions = new List<Type>();

        var result = CliTemplateCodeParser.Parse(
            "test.tst",
            "// ${using System;}\n$Classes[$Name]",
            extensions,
            errorReporter);

        // Commented code block should be preserved
        result.ShouldBe("// ${using System;}\n$Classes[$Name]");
    }

    [Fact]
    public void Parse_BacktickEscapedCodeBlock_PreservesEscapedBlock()
    {
        var errorReporter = CreateMockErrorReporter();
        var extensions = new List<Type>();

        var result = CliTemplateCodeParser.Parse(
            "test.tst",
            "`${not a code block}\n$Classes[$Name]",
            extensions,
            errorReporter);

        // Backtick-escaped code block should be preserved
        result.ShouldBe("`${not a code block}\n$Classes[$Name]");
    }
}

#endregion

#region Snippet Tests

public class SnippetTests
{
    [Fact]
    public void Create_WithTypeAndCode_ReturnsSnippet()
    {
        var snippet = Snippet.Create(SnippetType.Using, "using System;");

        snippet.Type.ShouldBe(SnippetType.Using);
        snippet.Code.ShouldBe("using System;");
    }

    [Fact]
    public void Create_WithAllParameters_SetsAllProperties()
    {
        var snippet = Snippet.Create(SnippetType.Code, "var x = 1;", 10, 5, 15);

        snippet.Type.ShouldBe(SnippetType.Code);
        snippet.Code.ShouldBe("var x = 1;");
        snippet.Offset.ShouldBe(10);
        snippet.StartIndex.ShouldBe(5);
        snippet.EndIndex.ShouldBe(15);
    }

    [Fact]
    public void Length_ReturnsCodeLength()
    {
        var snippet = Snippet.Create(SnippetType.Code, "test code");

        snippet.Length.ShouldBe(9);
    }
}

#endregion
