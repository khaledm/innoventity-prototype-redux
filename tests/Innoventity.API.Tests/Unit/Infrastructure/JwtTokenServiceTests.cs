using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Innoventity.API.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Innoventity.API.Tests.Unit.Infrastructure;

public class JwtTokenServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:SigningKey", "ThisIsATestSigningKeyWithAtLeast32CharactersForHS256Algorithm" },
            { "Jwt:Issuer", "InnoventityTestIssuer" },
            { "Jwt:Audience", "InnoventityTestAudience" },
            { "Jwt:AccessTokenExpirationMinutes", "60" },
            { "Jwt:RefreshTokenExpirationDays", "7" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _service = new JwtTokenService(_configuration);
    }

    [Fact]
    public void GenerateAccessToken_Should_CreateValidHS256Token()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var actorType = "IdeaGenerator";
        var email = "test@example.com";

        var signingKey = _configuration["Jwt:SigningKey"]!;
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));

        // Act
        var token = _service.GenerateAccessToken(actorId, actorType, email);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = securityKey,
            ValidateIssuer = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = _configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
        var jwtToken = validatedToken as JwtSecurityToken;

        Assert.NotNull(jwtToken);
        Assert.Equal(SecurityAlgorithms.HmacSha256, jwtToken.Header.Alg);
        Assert.Equal(actorId.ToString(), principal.FindFirst("actorId")?.Value);
        Assert.Equal(actorType, principal.FindFirst("actorType")?.Value);
        Assert.Equal(email, principal.FindFirst(ClaimTypes.Email)?.Value);
    }

    [Fact]
    public void GenerateAccessToken_Should_SetExpiryTo1Hour()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var actorType = "RD";
        var email = "rd@example.com";
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _service.GenerateAccessToken(actorId, actorType, email);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        var expectedExpiry = beforeGeneration.AddHours(1);
        var actualExpiry = jwtToken.ValidTo;

        // Allow 5 seconds tolerance for test execution time
        Assert.True(actualExpiry >= expectedExpiry.AddSeconds(-5) && actualExpiry <= expectedExpiry.AddSeconds(5),
            $"Expected expiry around {expectedExpiry:O}, but got {actualExpiry:O}");
    }

    [Fact]
    public void GenerateRefreshToken_Should_SetExpiryTo7Days()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _service.GenerateRefreshToken(actorId);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        var expectedExpiry = beforeGeneration.AddDays(7);
        var actualExpiry = jwtToken.ValidTo;

        // Allow 5 seconds tolerance for test execution time
        Assert.True(actualExpiry >= expectedExpiry.AddSeconds(-5) && actualExpiry <= expectedExpiry.AddSeconds(5),
            $"Expected expiry around {expectedExpiry:O}, but got {actualExpiry:O}");
    }
}
