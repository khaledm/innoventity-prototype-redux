using Innoventity.API.Domain.Entities;

namespace Innoventity.API.Tests.Unit.Domain.Entities.OwnedSections;

public class CollaborationRequirementTests
{
    [Fact]
    public void Default_Construction_Should_YieldEmptyPartnersNeeded()
    {
        var collaboration = new CollaborationRequirement();
        Assert.Equal(string.Empty, collaboration.PartnersNeeded);
    }

    [Fact]
    public void PartnersNeeded_Should_Accept_ExplicitNull()
    {
        var collaboration = new CollaborationRequirement { PartnersNeeded = null };
        Assert.Null(collaboration.PartnersNeeded);
    }

    [Fact]
    public void PartnersNeeded_Should_Accept_CommaSeparatedValues()
    {
        var collaboration = new CollaborationRequirement { PartnersNeeded = "RD,Manufacturing,SalesMarketing" };
        var parsed = collaboration.PartnersNeeded!.Split(',', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, parsed.Length);
        Assert.Contains("RD", parsed);
        Assert.Contains("Manufacturing", parsed);
        Assert.Contains("SalesMarketing", parsed);
    }

    [Fact]
    public void PartnersNeeded_Should_Accept_SinglePartnerType()
    {
        var collaboration = new CollaborationRequirement { PartnersNeeded = "Investor" };
        var parsed = collaboration.PartnersNeeded!.Split(',', StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(parsed);
        Assert.Equal("Investor", parsed[0]);
    }
}