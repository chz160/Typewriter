namespace TestFixtures.Models;

/// <summary>
/// Base interface for entities.
/// </summary>
public interface IEntity
{
    int Id { get; set; }
}

/// <summary>
/// Interface for auditable entities.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
}

/// <summary>
/// Model implementing multiple interfaces.
/// </summary>
public class AuditableEntity : IEntity, IAuditable
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
