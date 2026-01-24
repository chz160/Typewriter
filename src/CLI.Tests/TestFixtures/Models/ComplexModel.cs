using System.ComponentModel.DataAnnotations;

namespace TestFixtures.Models;

/// <summary>
/// Model status enumeration.
/// </summary>
public enum ModelStatus
{
    Draft,
    Active,
    Archived
}

/// <summary>
/// A complex model with nullable types, enums, and nested classes.
/// </summary>
[Serializable]
public class ComplexModel
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public ModelStatus Status { get; set; }

    public decimal Price { get; set; }

    public List<string> Tags { get; set; } = new();

    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Nested address class.
    /// </summary>
    public class Address
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    public Address? ShippingAddress { get; set; }
}
