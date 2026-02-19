namespace Innoventity.API.Domain.Common;

/// <summary>
/// Base class for entities using Guid as primary key
/// Used by: Actor, Innovation, FormalResponse (Phase 1+), BusinessPlan (Phase 2+)
/// </summary>
public abstract class EntityOfGuid : EntityBase<Guid>
{
    /// <summary>
    /// Parameterless constructor for EF Core
    /// </summary>
    protected EntityOfGuid() : base()
    {
    }

    /// <summary>
    /// Constructor with Id for derived classes and seed data
    /// </summary>
    protected EntityOfGuid(Guid id) : base(id)
    {
    }
}
