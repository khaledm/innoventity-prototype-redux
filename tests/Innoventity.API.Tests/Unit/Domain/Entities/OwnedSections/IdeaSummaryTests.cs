using System.ComponentModel.DataAnnotations;
using Innoventity.API.Domain.Entities;

namespace Innoventity.API.Tests.Unit.Domain.Entities.OwnedSections;
public class IdeaSummaryTests
{
    [Fact]
    public void IdeaSummary_Validate_Should_Reject_Null_Title()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var ideaSummary = new IdeaSummary
            {
                Title = null!,
                ProductType = "Valid Product Type",
                ResearchCategory = ResearchCategory.Management,
                IprStatus = "Valid IPR Status",
                ResearchBackground = "Valid Research Background"
            };

            ideaSummary.Validate();
        });

        Assert.Contains("Title is required.", exception.Message);
        Assert.Equal("Title", exception.ParamName);
    }

    [Fact]
    public void IdeaSummary_Validate_Should_Reject_Empty_ProductType()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
        {
            var ideaSummary = new IdeaSummary
            {
                Title = "Valid Title",
                ProductType = null!,
                ResearchCategory = ResearchCategory.Management,
                IprStatus = "Valid IPR Status",
                ResearchBackground = "Valid Research Background"
            };

            ideaSummary.Validate();
        });

        Assert.Contains("ProductType is required.", exception.Message);
        Assert.Equal("ProductType", exception.ParamName);
    }

    [Fact]
    public void IdeaSummary_Validate_Should_Accept_Valid_Data()
    {
        var ideaSummary = new IdeaSummary
        {
            Title = "Valid Title",
            ProductType = "Valid Product Type",
            ResearchCategory = ResearchCategory.Management,
            IprStatus = "Valid IPR Status",
            ResearchBackground = "Valid Research Background"
        };

        // Act & Assert - Should not throw any exceptions
        var exception = Record.Exception(() => ideaSummary.Validate());
        Assert.Null(exception);
    }

    [Fact]
    public void IdeaSummary_Validate_Should_Reject_Invalid_ResearchCategory_Enum()
    {
        var invalidCategory = (ResearchCategory)999; // Invalid enum value

        var ideaSummary = new IdeaSummary
        {
            Title = "Valid Title",
            ProductType = "Valid Product Type",
            ResearchCategory = invalidCategory,
            IprStatus = "Valid IPR Status",
            ResearchBackground = "Valid Research Background"
        };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            ideaSummary.Validate();
        });

        Assert.Contains("Invalid ResearchCategory value.", exception.Message);
    }

    private static IdeaSummary CreateValid(string title = "A Real Product Title", string? researchBackground = null) => new()
    {
        Title = title,
        ProductType = "Valid Product Type",
        ResearchCategory = ResearchCategory.Management,
        IprStatus = "Valid IPR Status",
        ResearchBackground = researchBackground ?? new string('a', 50)
    };

    [Theory]
    [InlineData("Untitled")]
    [InlineData("untitled project")]
    [InlineData("TODO")]
    [InlineData("Fix TODO later")]
    public void IsComplete_Should_Reject_Title_Containing_Either_Placeholder_Alone(string placeholderTitle)
    {
        var ideaSummary = CreateValid(placeholderTitle);

        Assert.False(ideaSummary.IsComplete());
    }

    [Fact]
    public void IsComplete_Should_Reject_Title_Containing_Both_Placeholders()
    {
        var ideaSummary = CreateValid("Untitled TODO");

        Assert.False(ideaSummary.IsComplete());
    }

    [Fact]
    public void IsComplete_Should_Accept_Title_Without_Placeholders()
    {
        var ideaSummary = CreateValid();

        Assert.True(ideaSummary.IsComplete());
    }

    [Fact]
    public void IsComplete_Should_Reject_ResearchBackground_Under_50_Chars()
    {
        var ideaSummary = CreateValid(researchBackground: new string('a', 49));

        Assert.False(ideaSummary.IsComplete());
    }

    [Fact]
    public void IsComplete_Should_Accept_ResearchBackground_At_Exactly_50_Chars()
    {
        var ideaSummary = CreateValid(researchBackground: new string('a', 50));

        Assert.True(ideaSummary.IsComplete());
    }
}