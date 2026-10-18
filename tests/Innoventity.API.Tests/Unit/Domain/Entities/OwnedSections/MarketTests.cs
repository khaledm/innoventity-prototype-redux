using Innoventity.API.Domain.Entities;

namespace Innoventity.API.Tests.Unit.Domain.Entities.OwnedSections;

public class MarketTests
{
    [Fact]
    public void Market_Validate_Should_Reject_Null_TargetMarket()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var market = new Market
            {
                TargetMarket = null!,
                TargetCustomerBase = "Valid Target Customer Base",
                TargetCustomerType = "Valid Target Customer Type",
                RelevantMarketSize = 1000000m,
                PotentialMarketSize = 5000000m
            };

            market.Validate();
        });

        Assert.Equal("TargetMarket", exception.ParamName);
        Assert.Contains("TargetMarket is required.", exception.Message);
    }

    [Fact]
    public void Market_Validate_Should_Accept_Optional_RelevantMarketSize()
    {
        var market = new Market
        {
            TargetMarket = "Valid Target Market",
            TargetCustomerBase = "Valid Target Customer Base",
            TargetCustomerType = "Valid Target Customer Type",
            RelevantMarketSize = null,
            PotentialMarketSize = 5000000m
        };

        // Act & Assert
        var exception = Record.Exception(() => market.Validate());
        Assert.Null(exception); // No exception should be thrown for valid data
    }

    [Fact]
    public void Market_Validate_Should_Reject_Invalid_TargetCustomerType()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var market = new Market
            {
                TargetMarket = "Valid Target Market",
                TargetCustomerBase = "Valid Target Customer Base",
                TargetCustomerType = null!,
                RelevantMarketSize = 1000000m,
                PotentialMarketSize = 5000000m
            };

            market.Validate();
        });

        Assert.Equal("TargetCustomerType", exception.ParamName);
        Assert.Contains("TargetCustomerType is required.", exception.Message);
    }

    private static Market CreateValid(
    decimal? relevantMarketSize = 1000000m,
    decimal? potentialMarketSize = 5000000m) => new()
    {
        TargetMarket = "Valid Target Market",
        TargetCustomerBase = "Valid Target Customer Base",
        TargetCustomerType = "Valid Target Customer Type",
        RelevantMarketSize = relevantMarketSize,
        PotentialMarketSize = potentialMarketSize
    };

    [Fact]
    public void IsComplete_Should_Accept_BothSizesPositive()
    {
        var market = CreateValid();
        Assert.True(market.IsComplete());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-1")]
    public void IsComplete_Should_Reject_RelevantMarketSize_NotPositive(string? value)
    {
        var market = CreateValid(relevantMarketSize: value == null ? null : decimal.Parse(value));
        Assert.False(market.IsComplete());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-1")]
    public void IsComplete_Should_Reject_PotentialMarketSize_NotPositive(string? value)
    {
        var market = CreateValid(potentialMarketSize: value == null ? null : decimal.Parse(value));
        Assert.False(market.IsComplete());
    }

    [Fact]
    public void IsComplete_Should_Accept_SmallestValidPositiveSize()
    {
        // Boundary case: kills a `> 0` → `>= 0` mutant
        var market = CreateValid(relevantMarketSize: 0.01m, potentialMarketSize: 0.01m);
        Assert.True(market.IsComplete());
    }
}