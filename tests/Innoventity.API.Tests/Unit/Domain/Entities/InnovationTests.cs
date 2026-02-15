using Innoventity.API.Domain.Entities;
using Xunit;

namespace Innoventity.API.Tests.Unit.Domain.Entities;

/// <summary>
/// PHASE 1 WORK - Domain validation tests deferred to Phase 1 (Post Phase 0.6)
/// These tests validate Innovation entity field requirements using [Required] attributes.
/// Expected failures in Phase 0.6 implementation as domain validation not yet implemented.
/// Will be addressed in Phase 1 when domain layer is enhanced with data annotations/FluentValidation.
/// </summary>
public class InnovationTests
{
    /// <summary>
    /// PHASE 1 DEFERRED: Validates Title is required
    /// </summary>
    [Fact]
    public void Innovation_Should_RequireTitle()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var innovation = new Innovation
            {
                Title = null!, // Explicitly set to null to test [Required] validation
                ProductType = "Energy Storage Device",
                ResearchBackground = "Test background",
                IprStatus = "Patent Pending",
                ProductDescription = "Test description",
                ProductAdvantages = "Test advantages",
                DevelopmentPhase = "Prototype",
                DevelopmentProcess = "Test process",
                TargetMarket = "Test market",
                TargetCustomerBase = "Test customer",
                TargetCustomerType = "B2B",
                ProductKeywords = "test",
                AdvantageKeywords = "test"
            };
        });
        Assert.Contains("Title", exception.Message);
    }

    /// <summary>
    /// PHASE 1 DEFERRED: Validates ProductType is required
    /// </summary>
    [Fact]
    public void Innovation_Should_RequireProductType()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var innovation = new Innovation
            {
                Title = "Test Innovation",
                ProductType = null!, // Explicitly set to null to test [Required] validation
                ResearchBackground = "Test background",
                IprStatus = "Patent Pending",
                ProductDescription = "Test description",
                ProductAdvantages = "Test advantages",
                DevelopmentPhase = "Prototype",
                DevelopmentProcess = "Test process",
                TargetMarket = "Test market",
                TargetCustomerBase = "Test customer",
                TargetCustomerType = "B2B",
                ProductKeywords = "test",
                AdvantageKeywords = "test"
            };
        });

        Assert.Contains("ProductType", exception.Message);
    }

    /// <summary>
    /// PHASE 1 DEFERRED: Validates ResearchBackground is required
    /// </summary>
    [Fact]
    public void Innovation_Should_RequireResearchBackground()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var innovation = new Innovation
            {
                Title = "Test Innovation",
                ProductType = "Energy Storage Device",
                ResearchBackground = null!, // Explicitly set to null to test [Required] validation
                IprStatus = "Patent Pending",
                ProductDescription = "Test description",
                ProductAdvantages = "Test advantages",
                DevelopmentPhase = "Prototype",
                DevelopmentProcess = "Test process",
                TargetMarket = "Test market",
                TargetCustomerBase = "Test customer",
                TargetCustomerType = "B2B",
                ProductKeywords = "test",
                AdvantageKeywords = "test"
            };
        });
#pragma warning restore CS9035

        Assert.Contains("ResearchBackground", exception.Message);
    }

    [Fact]
    public void Innovation_Should_AllowValidConstruction()
    {
        // Arrange & Act
        var innovation = new Innovation(Guid.NewGuid())
        {
            IdeaToken = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Title = "Quantum Battery Prototype",
            ProductType = "Energy Storage Device",
            ResearchBackground = "Lithium-air battery leveraging quantum tunneling",
            ResearchCategory = ResearchCategory.Engineering,
            IprStatus = "Patent Pending",
            ProductDescription = "Next-generation battery technology",
            ProductAdvantages = "10x energy density, 50% faster charging",
            DevelopmentPhase = "Prototype",
            DevelopmentProcess = "Laboratory validation complete",
            TargetMarket = "Electric vehicle manufacturers",
            TargetCustomerBase = "Automotive OEMs",
            TargetCustomerType = "B2B",
            ProductKeywords = "battery, energy storage, electric vehicle",
            AdvantageKeywords

 = "energy density, fast charging",
            Status = InnovationStatus.Published,
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(innovation);
        Assert.Equal("Quantum Battery Prototype", innovation.Title);
        Assert.Equal(ResearchCategory.Engineering, innovation.ResearchCategory);
        Assert.Equal(InnovationStatus.Published, innovation.Status);
    }

    [Fact]
    public void Innovation_Should_InitializeTargetIndustriesCollection()
    {
        // Arrange & Act
        var innovation = new Innovation
        {
            Title = "Test Innovation",
            ProductType = "Energy Storage Device",
            ResearchBackground = "Test background",
            IprStatus = "Patent Pending",
            ProductDescription = "Test description",
            ProductAdvantages = "Test advantages",
            DevelopmentPhase = "Prototype",
            DevelopmentProcess = "Test process",
            TargetMarket = "Test market",
            TargetCustomerBase = "Test customer",
            TargetCustomerType = "B2B",
            ProductKeywords = "test",
            AdvantageKeywords = "test"
        };

        // Assert
        Assert.NotNull(innovation.TargetIndustries);
        Assert.Empty(innovation.TargetIndustries);
    }
}
