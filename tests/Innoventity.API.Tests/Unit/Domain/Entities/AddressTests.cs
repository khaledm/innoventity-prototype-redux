using Innoventity.API.Domain.Entities;

namespace Innoventity.API.Tests.Unit.Domain.Entities;

public class AddressTests
{
    [Fact]
    public void Address_WithValidData_ShouldCreate()
    {
        // Arrange & Act
        var address = new Address
        {
            Address1 = "123 Innovation Drive",
            Address2 = "Suite 100",
            City = "Tech City",
            PostCode = "TC 12345",
            CountryCode = "US"
        };

        // Assert
        Assert.NotNull(address);
        Assert.Equal("123 Innovation Drive", address.Address1);
        Assert.Equal("Suite 100", address.Address2);
        Assert.Equal("Tech City", address.City);
        Assert.Equal("TC 12345", address.PostCode);
        Assert.Equal("US", address.CountryCode);
    }

    [Fact]
    public void Address_CountryCode_ShouldBe2Characters()
    {
        // Arrange & Act
        var address = new Address
        {
            Address1 = "123 Innovation Drive",
            City = "Tech City",
            PostCode = "TC 12345",
            CountryCode = "GB"  // ISO 3166-1 alpha-2 format
        };

        // Assert
        Assert.Equal(2, address.CountryCode.Length);
        Assert.Equal("GB", address.CountryCode);
    }

    [Fact]
    public void Address_RequiredFields_MustBeProvided()
    {
        // Arrange & Act - Create address with all required fields
        var address = new Address
        {
            Address1 = "456 Development Street",
            City = "Innovation City",
            PostCode = "IC 67890",
            CountryCode = "DE"
        };

        // Assert - Verify all required fields are set
        Assert.NotNull(address.Address1);
        Assert.NotNull(address.City);
        Assert.NotNull(address.PostCode);
        Assert.NotNull(address.CountryCode);
        Assert.False(string.IsNullOrWhiteSpace(address.Address1));
        Assert.False(string.IsNullOrWhiteSpace(address.City));
        Assert.False(string.IsNullOrWhiteSpace(address.PostCode));
        Assert.False(string.IsNullOrWhiteSpace(address.CountryCode));
    }

    [Fact]
    public void Address_OptionalFields_CanBeNull()
    {
        // Arrange & Act - Create address without optional Address2
        var address = new Address
        {
            Address1 = "789 Main Boulevard",
            City = "Metro City",
            PostCode = "MC 11111",
            CountryCode = "FR"
        };

        // Assert
        Assert.Null(address.Address2);
    }

    [Fact]
    public void Address_WithInternationalFormats_ShouldWork()
    {
        // Arrange & Act - Test various international address formats
        var ukAddress = new Address
        {
            Address1 = "10 Downing Street",
            City = "London",
            PostCode = "SW1A 2AA",
            CountryCode = "GB"
        };

        var germanAddress = new Address
        {
            Address1 = "Unter den Linden 77",
            City = "Berlin",
            PostCode = "10117",
            CountryCode = "DE"
        };

        // Assert
        Assert.Equal("GB", ukAddress.CountryCode);
        Assert.Equal("SW1A 2AA", ukAddress.PostCode);
        Assert.Equal("DE", germanAddress.CountryCode);
        Assert.Equal("10117", germanAddress.PostCode);
    }
}
