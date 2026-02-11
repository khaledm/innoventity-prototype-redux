using Innoventity.API.Domain.Entities;
using Xunit;

namespace Innoventity.API.Tests.Unit.Domain.Entities;

public class InnovationTests
{
    [Fact]
    public void Innovation_Should_RequireTitle()
    {
#pragma warning disable CS9035 // Required member must be set
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var innovation = new Innovation
            {
                // Title omitted - should fail
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
#pragma warning restore CS9035
        Assert.Contains("Title", exception.Message);
    }

    [Fact]
    public void Innovation_Should_RequireProductType()
    {
#pragma warning disable CS9035 // Required member must be set
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var innovation = new Innovation
            {
                Title = "Test Innovation",
                // ProductType omitted - should fail
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
#pragma warning restore CS9035

        Assert.Contains("ProductType", exception.Message);
    }

    [Fact]
    public void Innovation_Should_RequireResearchBackground()
    {
#pragma warning disable CS9035 // Required member must be set
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var innovation = new Innovation
            {
                Title = "Test Innovation",
                ProductType = "Energy Storage Device",
                // ResearchBackground omitted - should fail
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
        var innovation = new Innovation
        {
            Id = Guid.NewGuid(),
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
