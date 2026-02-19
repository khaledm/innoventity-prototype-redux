namespace Innoventity.API.Domain.Common;

/// <summary>
/// Base class for entities using int as primary key
/// Used by: Industry (if converting from string PK), child entities in Phase 2+
/// </summary>
public abstract class EntityOfInt32 : EntityBase<int>
{
    // Inherits all equality semantics from EntityBase<int>
    // No additional implementation needed
}
