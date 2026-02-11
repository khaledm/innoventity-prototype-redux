using System.Runtime.CompilerServices;

namespace Innoventity.API.Domain.Common;

/// <summary>
/// Base class for all domain entities providing identity-based equality semantics (R9.1)
/// Implements DDD Entity pattern from Evans "Domain-Driven Design" Chapter 5
/// </summary>
/// <typeparam name="TId">Type of the entity identifier (Guid, int, string, etc.)</typeparam>
public abstract class EntityBase<TId> : IEquatable<EntityBase<TId>>
{
    /// <summary>
    /// Unique identifier for the entity
    /// Protected setter ensures Id is set only during entity construction or by ORM
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Parameterless constructor for EF Core
    /// </summary>
    protected EntityBase()
    {
    }

    /// <summary>
    /// Constructor with Id for derived classes and seed data
    /// </summary>
    /// <param name="id">Entity identifier</param>
    protected EntityBase(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// Determines if entity is transient (not yet persisted to database)
    /// Transient entities have Id equal to default(TId)
    /// </summary>
    public bool IsTransient()
    {
        return EqualityComparer<TId>.Default.Equals(Id, default!);
    }

    /// <summary>
    /// Identity-based equality: Two entities are equal if they have the same Id
    /// Transient entities are equal only if they are the same object reference
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not EntityBase<TId> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        // Transient entities are never equal (except by reference above)
        if (IsTransient() || other.IsTransient())
            return false;

        // Both entities have Ids - compare by Id only
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public bool Equals(EntityBase<TId>? other)
    {
        return Equals((object?)other);
    }

    /// <summary>
    /// Stable hash code for use in collections (HashSet, Dictionary)
    /// Transient entities use reference-based hash (RuntimeHelpers.GetHashCode)
    /// Persisted entities use Id-based hash
    /// </summary>
    public override int GetHashCode()
    {
        // For transient entities, use runtime reference hash (stable for lifetime)
        if (IsTransient())
            return RuntimeHelpers.GetHashCode(this);

        // For persisted entities, use Id hash (stable across lookups)
        return Id!.GetHashCode();
    }

    public static bool operator ==(EntityBase<TId>? left, EntityBase<TId>? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(EntityBase<TId>? left, EntityBase<TId>? right)
    {
        return !(left == right);
    }
}
