using Innoventity.API.Domain.Entities;

namespace Innoventity.API.Tests.Unit.Domain.Entities;

public class InnovationTests
{
    private static Innovation CreateValidInnovation(Guid? id = null)
    {
        return new Innovation(id ?? Guid.NewGuid())
        {
            IdeaToken = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            IdeaSummary = new IdeaSummary
            {
                Title = "Quantum Battery Prototype",
                ProductType = "Energy Storage Device",
                ResearchBackground = "Lithium-air battery leveraging quantum tunneling for enhanced storage",
                ResearchCategory = ResearchCategory.Engineering,
                IprStatus = "Patent Pending"
            },
            Product = new Product
            {
                ProductDescription = "Next-generation battery technology",
                TechnologyDescription = "Quantum tunneling mechanism for enhanced energy storage",
                ProductAdvantages = "10x energy density, 50% faster charging",
                DevelopmentPhase = "Prototype",
                DevelopmentProcess = "Laboratory validation complete",
                TargetBeneficiaries = "Electric vehicle manufacturers and renewable energy providers",
                ProductKeywords = "battery, energy storage, electric vehicle",
                AdvantageKeywords = "energy density, fast charging"
            },
            Market = new Market
            {
                TargetMarket = "Electric vehicle manufacturers",
                TargetCustomerBase = "Automotive OEMs",
                TargetCustomerType = "B2B",
                RelevantMarketSize = 50000000000m,
                PotentialMarketSize = 150000000000m
            },
            CollaborationRequirement = new CollaborationRequirement
            {
                PartnersNeeded = "RD,Manufacturing"
            },
            Status = InnovationStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow,
            SubmittedAt = DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public void Innovation_Should_AllowValidConstruction()
    {
        var innovation = CreateValidInnovation();

        Assert.NotNull(innovation);
        Assert.Equal("Quantum Battery Prototype", innovation.IdeaSummary.Title);
        Assert.Equal(ResearchCategory.Engineering, innovation.IdeaSummary.ResearchCategory);
        Assert.Equal(InnovationStatus.Published, innovation.Status);
    }

    [Fact]
    public void Innovation_Should_InitializeTargetIndustriesCollection()
    {
        var innovation = CreateValidInnovation();

        Assert.NotNull(innovation.TargetIndustries);
        Assert.Empty(innovation.TargetIndustries);
    }

    [Fact]
    public void Innovation_Should_Preserve_Root_Properties()
    {
        var id = Guid.NewGuid();
        var innovation = CreateValidInnovation(id);

        Assert.Equal(id, innovation.Id);
        Assert.NotEqual(Guid.Empty, innovation.IdeaToken);
        Assert.NotEqual(Guid.Empty, innovation.OwnerId);
        Assert.Equal(InnovationStatus.Published, innovation.Status);
        Assert.NotEqual(default, innovation.CreatedAt);
        Assert.NotNull(innovation.SubmittedAt);
    }

    [Fact]
    public void Innovation_Should_ReportComplete_When_AllOwnedSectionsComplete()
    {
        var innovation = CreateValidInnovation();
        innovation.TargetIndustries.Add(new Industry("TECH-001") { Name = "Technology" });

        Assert.True(innovation.IsIdeaSummaryComplete());
        Assert.True(innovation.IsProductDetailsComplete());
        Assert.True(innovation.IsMarketDetailsSectionComplete());
        Assert.True(innovation.IsReadyForSubmission());
    }

    [Fact]
    public void Innovation_Should_NotBeReadyForSubmission_When_NoTargetIndustries()
    {
        var innovation = CreateValidInnovation();

        Assert.False(innovation.IsReadyForSubmission());
    }

    [Fact]
    public void Innovation_Should_NotBeProductDetailsComplete_When_ProductDescriptionMissing()
    {
        var innovation = CreateValidInnovation();
        innovation.TargetIndustries.Add(new Industry("TECH-001")
        {
            Name = "Technology"
        });
        innovation.Product = new Product
        {
            ProductDescription = "", // broken
            TechnologyDescription = "Channel tunneling mechanism for enhanced energy storage",
            ProductAdvantages = "10x energy density",
            DevelopmentPhase = "Prototype",
            DevelopmentProcess = "Laboratory validation complete",
            TargetBeneficiaries = "Electric vehicle manufacturers",
            ProductKeywords = "battery",
            AdvantageKeywords = "energy density"
        };

        Assert.True(innovation.IsIdeaSummaryComplete());
        Assert.False(innovation.IsProductDetailsComplete());
        Assert.False(innovation.IsReadyForSubmission());
    }

    [Fact]
    public void Innovation_Should_NotBeMArketDetailsSectionComplete_When_RelevantMarketSizeIsZero()
    {
        var innovation = CreateValidInnovation();
        innovation.TargetIndustries.Add(new Industry("TECH-001") { Name = "Technology" });
        innovation.Market = new Market
        {
            TargetMarket = "Electric vehicle manufacturers",
            TargetCustomerBase = "Automotive OEMs",
            TargetCustomerType = "B2B",
            RelevantMarketSize = 0m,  // broken
            PotentialMarketSize = 150000000000m
        };

        Assert.True(innovation.IsProductDetailsComplete());
        Assert.False(innovation.IsMarketDetailsSectionComplete());
        Assert.False(innovation.IsReadyForSubmission());
    }

    [Fact]
    public void Innovation_Should_NotBeReadyForSubmission_When_PartnersNeededIsWhitespace()
    {
        var innovation = CreateValidInnovation();
        innovation.TargetIndustries.Add(new Industry("TECH-001") { Name = "Technology" });
        innovation.CollaborationRequirement = new CollaborationRequirement { PartnersNeeded = "       " };

        Assert.False(innovation.IsReadyForSubmission());
    }
}
