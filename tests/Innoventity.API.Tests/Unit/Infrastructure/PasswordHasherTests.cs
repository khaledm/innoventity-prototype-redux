using Innoventity.API.Infrastructure.Authentication;

namespace Innoventity.API.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for PasswordHasher service (R8.4 Account Security)
/// </summary>
public class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new PasswordHasher();
    }

    [Fact]
    public void HashPassword_UsesWorkFactor12_R8_4()
    {
        // Arrange
        var password = "SecureP@ssw0rd!";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        // BCrypt work factor 12 hashes start with "$2a$12$" or "$2b$12$"
        Assert.Matches(@"^\$2[ab]\$12\$", hash);
    }

    [Fact]
    public void HashPassword_GeneratesUniqueHashesForSamePassword()
    {
        // Arrange
        var password = "SecureP@ssw0rd!";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert - BCrypt includes random salt, so hashes should differ
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_ReturnsTrueForValidPassword()
    {
        // Arrange
        var password = "SecureP@ssw0rd!";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForInvalidPassword()
    {
        // Arrange
        var password = "SecureP@ssw0rd!";
        var wrongPassword = "WrongP@ssw0rd!";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HashPassword_HandlesMinimumComplexityPassword()
    {
        // Arrange - minimum R8.4 complexity: 8 chars, upper, lower, digit, special
        var password = "Abcd123!";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.Matches(@"^\$2[ab]\$12\$", hash);
    }

    [Fact]
    public void HashPassword_HandlesMaximumLengthPassword()
    {
        // Arrange - R8.4 maximum: 128 characters
        var password = new string('A', 124) + "a1!"; // 124 + 4 = 128

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }
}
