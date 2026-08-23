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
}
