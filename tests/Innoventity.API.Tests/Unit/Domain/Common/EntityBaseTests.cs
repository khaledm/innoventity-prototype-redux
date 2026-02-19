using Innoventity.API.Domain.Common;
using System.Runtime.CompilerServices;

namespace Innoventity.API.Tests.Unit.Domain.Common;

/// <summary>
/// Unit tests for EntityBase<TId> equality semantics (R9.1)
/// Tests identity-based equality per DDD principles
/// </summary>
public class EntityBaseTests
{
    // Test entity for validation
    private class TestEntity : EntityBase<Guid>
    {
        public TestEntity(Guid id)
        {
            Id = id;
        }

        public TestEntity() { } // Transient entity
    }

    [Fact]
    public void Entities_WithSameId_ShouldBeEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act
        var areEqual = entity1.Equals(entity2);
        var operatorEqual = entity1 == entity2;

        // Assert
        Assert.True(areEqual, "Entities with same Id should be equal");
        Assert.True(operatorEqual, "== operator should respect identity equality");
        Assert.Equal(entity2.GetHashCode(), entity1.GetHashCode());
    }

    [Fact]
    public void Entities_WithDifferentId_ShouldNotBeEqual()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        // Act
        var areEqual = entity1.Equals(entity2);
        var operatorNotEqual = entity1 != entity2;

        // Assert
        Assert.False(areEqual, "Entities with different Ids should not be equal");
        Assert.True(operatorNotEqual, "!= operator should detect different Ids");
    }

    [Fact]
    public void TransientEntities_ShouldOnlyBeEqualByReference()
    {
        // Arrange - entities without Ids (transient)
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();
        var entity3 = entity1; // Same reference

        // Act
        var sameReference = entity1.Equals(entity3);
        var differentReference = entity1.Equals(entity2);

        // Assert
        Assert.True(sameReference, "Same reference should be equal");
        Assert.False(differentReference, "Transient entities with different references should not be equal");
    }

    [Fact]
    public void IsTransient_WhenIdIsDefault_ShouldReturnTrue()
    {
        // Arrange
        var transientEntity = new TestEntity();

        // Act
        var isTransient = transientEntity.IsTransient();

        // Assert
        Assert.True(isTransient, "Entity with default(Guid) Id should be transient");
    }

    [Fact]
    public void IsTransient_WhenIdIsAssigned_ShouldReturnFalse()
    {
        // Arrange
        var persistedEntity = new TestEntity(Guid.NewGuid());

        // Act
        var isTransient = persistedEntity.IsTransient();

        // Assert
        Assert.False(isTransient, "Entity with assigned Id should not be transient");
    }

    [Fact]
    public void GetHashCode_ForPersistedEntity_ShouldBeStable()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new TestEntity(id);

        // Act
        var hashCode1 = entity.GetHashCode();
        var hashCode2 = entity.GetHashCode();

        // Add to HashSet to verify stability in collections
        var hashSet = new HashSet<TestEntity> { entity };
        var containsEntity = hashSet.Contains(entity);

        // Assert
        Assert.Equal(hashCode2, hashCode1);
        Assert.True(containsEntity, "Entity should be found in HashSet using stable hash code");
    }

    [Fact]
    public void GetHashCode_ForTransientEntity_ShouldUseRuntimeHash()
    {
        // Arrange
        var transientEntity = new TestEntity();

        // Act
        var hashCode = transientEntity.GetHashCode();
        var runtimeHashCode = RuntimeHelpers.GetHashCode(transientEntity);

        // Assert
        Assert.Equal(runtimeHashCode, hashCode);
    }

    [Fact]
    public void EqualityOperator_WithNullEntities_ShouldHandleCorrectly()
    {
        // Arrange
        TestEntity? nullEntity1 = null;
        TestEntity? nullEntity2 = null;
        var entity = new TestEntity(Guid.NewGuid());

        // Act & Assert
        Assert.True(nullEntity1 == nullEntity2, "Two nulls should be equal");
        Assert.False(entity == null, "Entity should not equal null");
        Assert.False(null == entity, "Null should not equal entity");
    }
}
