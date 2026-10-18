using Innoventity.API.Domain.Entities;

namespace Innoventity.API.Tests.Unit.Domain.Entities.OwnedSections;

public class ProductTests
{
    [Fact]
    public void Product_Validate_Should_Reject_Null_ProductDescription()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var product = new Product
            {
                ProductDescription = null!,
                TechnologyDescription = "Valid Technology Description",
                ProductAdvantages = "Valid Product Advantages",
                DevelopmentPhase = "Valid Development Phase",
                DevelopmentProcess = "Valid Development Process",
                TargetBeneficiaries = "Valid Target Beneficiaries",
                ProductKeywords = "Valid Product Keywords",
                AdvantageKeywords = "Valid Advantage Keywords"
            };

            product.Validate();
        });

        Assert.Equal("ProductDescription", exception.ParamName);
        Assert.Contains("ProductDescription is required.", exception.Message);
    }

    [Fact]
    public void Product_Validate_Should_Reject_Null_TechnologyDescription()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var product = new Product
            {
                ProductDescription = "Valid Product Description",
                TechnologyDescription = null!,
                ProductAdvantages = "Valid Product Advantages",
                DevelopmentPhase = "Valid Development Phase",
                DevelopmentProcess = "Valid Development Process",
                TargetBeneficiaries = "Valid Target Beneficiaries",
                ProductKeywords = "Valid Product Keywords",
                AdvantageKeywords = "Valid Advantage Keywords"
            };

            product.Validate();
        });

        Assert.Equal("TechnologyDescription", exception.ParamName);
        Assert.Contains("TechnologyDescription is required.", exception.Message);
    }

    [Fact]
    public void Product_Validate_Should_Accept_Optional_ProductAdvantages()
    {
        var product = new Product
        {
            ProductDescription = "Valid Product Description",
            TechnologyDescription = "Valid Technology Description",
            ProductAdvantages = null!, // Optional field
            DevelopmentPhase = "Valid Development Phase",
            DevelopmentProcess = "Valid Development Process",
            TargetBeneficiaries = "Valid Target Beneficiaries",
            ProductKeywords = "Valid Product Keywords",
            AdvantageKeywords = "Valid Advantage Keywords"
        };

        var exception = Record.Exception(() => product.Validate());
        Assert.Null(exception);
        Assert.Null(product.ProductAdvantages);
    }

    [Fact]
    public void Product_Validate_Should_Reject_Invalid_DevelopmentPhase()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var product = new Product
            {
                ProductDescription = "Valid Product Description",
                TechnologyDescription = "Valid Technology Description",
                ProductAdvantages = "Valid Product Advantages",
                DevelopmentPhase = null!,
                DevelopmentProcess = "Valid Development Process",
                TargetBeneficiaries = "Valid Target Beneficiaries",
                ProductKeywords = "Valid Product Keywords",
                AdvantageKeywords = "Valid Advantage Keywords"
            };

            product.Validate();
        });
        Assert.Equal("DevelopmentPhase", exception.ParamName);
        Assert.Contains("DevelopmentPhase is required.", exception.Message);
    }

    [Fact]
    public void Product_Validate_Should_Trim_ProductKeywords()
    {
        var product = new Product
        {
            ProductDescription = "Valid Product Description",
            TechnologyDescription = "Valid Technology Description",
            ProductAdvantages = "Valid Product Advantages",
            DevelopmentPhase = "Valid Development Phase",
            DevelopmentProcess = "Valid Development Process",
            TargetBeneficiaries = "Valid Target Beneficiaries",
            ProductKeywords = "   Valid Product Keywords   ",
            AdvantageKeywords = "Valid Advantage Keywords"
        };

        var exception = Record.Exception(() => product.Validate());
        Assert.Null(exception);

        Assert.Equal("Valid Product Keywords", product.ProductKeywords);
    }

    private static Product CreateValid(
    string productDescription = "Valid Product Description",
    string technologyDescription = "Valid Technology Description",
    string targetBeneficiaries = "Valid Target Beneficiaries") => new()
    {
        ProductDescription = productDescription,
        TechnologyDescription = technologyDescription,
        ProductAdvantages = "Valid Product Advantages",
        DevelopmentPhase = "Valid Development Phase",
        DevelopmentProcess = "Valid Development Process",
        TargetBeneficiaries = targetBeneficiaries,
        ProductKeywords = "Valid Product Keywords",
        AdvantageKeywords = "Valid Advantage Keywords"
    };

    [Fact]
    public void IsComplete_Should_Accept_AllThreeRequiredFieldsPresent()
    {
        var product = CreateValid();
        Assert.True(product.IsComplete());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsComplete_Should_Reject_Empty_ProductDescription(string? value)
    {
        var product = CreateValid(productDescription: value!);
        Assert.False(product.IsComplete());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsComplete_Should_Reject_Empty_TechnologyDescription(string? value)
    {
        var product = CreateValid(technologyDescription: value!);
        Assert.False(product.IsComplete());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsComplete_Should_Reject_Empty_TargetBeneficiaries(string? value)
    {
        var product = CreateValid(targetBeneficiaries: value!);
        Assert.False(product.IsComplete());
    }

    [Fact]
    public void IsComplete_Should_Ignore_OptionalFields_WhenEmpty()
    {
        // ProductAdvantages/DevelopmentPhase/DevelopmentProcess/keywords are not
        // part of the completeness rule (rules 7-9 only) — confirms IsComplete()
        // doesn't accidentally over-check fields Validate() requires but the
        // publish gate doesn't.
        var product = new Product
        {
            ProductDescription = "Valid Product Description",
            TechnologyDescription = "Valid Technology Description",
            ProductAdvantages = null!,
            DevelopmentPhase = null!,
            DevelopmentProcess = null!,
            TargetBeneficiaries = "Valid Target Beneficiaries",
            ProductKeywords = null!,
            AdvantageKeywords = null!
        };

        Assert.True(product.IsComplete());
    }
}
