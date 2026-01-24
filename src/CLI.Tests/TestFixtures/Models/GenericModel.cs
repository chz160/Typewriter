namespace TestFixtures.Models;

/// <summary>
/// A generic model for testing generic type handling.
/// </summary>
public class GenericModel<T>
{
    public T Value { get; set; } = default!;
    public List<T> Items { get; set; } = new();
}

/// <summary>
/// Model that inherits from a generic base class.
/// </summary>
public class StringModel : GenericModel<string>
{
    public string Description { get; set; } = string.Empty;
}
