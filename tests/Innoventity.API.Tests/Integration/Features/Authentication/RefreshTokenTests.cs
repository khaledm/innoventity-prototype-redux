using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Persistence;
using Innoventity.API.Infrastructure.Authentication;

namespace Innoventity.API.Tests.Integration.Features.Authentication;

public class RefreshTokenTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factoryBase;

    public RefreshTokenTests(WebApplicationFactory<Program> factory)
    {
        _factoryBase = factory;
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        return _factoryBase.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace AppDbContext with in-memory database
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                var dbName = $"RefreshTokenTests_{Guid.NewGuid()}";
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });

            // Configure JWT settings for tests
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:SigningKey", "ThisIsATestSigningKeyWithAtLeast32CharactersForHS256Algorithm" },
                    { "Jwt:Issuer", "InnoventityTestIssuer" },
                    { "Jwt:Audience", "InnoventityTestAudience" },
                    { "Jwt:AccessTokenExpirationMinutes", "60" },
                    { "Jwt:RefreshTokenExpirationDays", "7" }
                });
            });
        });
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewAccessToken()
    {
        // Arrange: Create factory and client
        var factory = CreateFactory();
        var client = factory.CreateClient();

        // Seed actor and generate valid refresh token
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jwtService = scope.ServiceProvider.GetRequiredService<JwtTokenService>();

        var actor = new Actor
        {
            Id = Guid.NewGuid(),
            Email = "refresh@example.com",
            FullName = "Refresh Test User",
            ContactAddress = "123 Test St",
            ActorType = ActorType.Investor,
            AccountStatus = AccountStatus.Active,
            PasswordHash = "dummy_hash",
            ActivationToken = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Actors.Add(actor);
        await dbContext.SaveChangesAsync();

        var validRefreshToken = jwtService.GenerateRefreshToken(actor.Id);

        var refreshRequest = new
        {
            refreshToken = validRefreshToken
        };

        // Act
        var response = await client.PostAsJsonAsync("/auth/refresh-token", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var refreshResponse = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        Assert.NotNull(refreshResponse);
        Assert.NotNull(refreshResponse.AccessToken);
        Assert.Equal(3600, refreshResponse.ExpiresIn);
        Assert.Equal("Bearer", refreshResponse.TokenType);
    }

    [Fact]
    public async Task RefreshToken_WithExpiredToken_ShouldReturn401()
    {
        // Arrange: Create factory and client
        var factory = CreateFactory();
        var client = factory.CreateClient();

        // Create an expired refresh token (manually crafted with past expiry)
        var expiredRefreshToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhY3RvcklkIjoiMTIzNDU2NzgtYWJjZC0xMjM0LWFiY2QtMTIzNDU2Nzg5MGFiIiwidG9rZW5UeXBlIjoicmVmcmVzaCIsImp0aSI6ImFiY2RlZmdoLTEyMzQtNTY3OC05MGFiLWNkZWYxMjM0NTY3OCIsIm5iZiI6MTYwOTQ1OTIwMCwiZXhwIjoxNjA5NDU5MjAwLCJpYXQiOjE2MDk0NTkyMDAsImlzcyI6Iklubm92ZW50aXR5VGVzdElzc3VlciIsImF1ZCI6Iklubm92ZW50aXR5VGVzdEF1ZGllbmNlIn0.dummy";

        var refreshRequest = new
        {
            refreshToken = expiredRefreshToken
        };

        // Act
        var response = await client.PostAsJsonAsync("/auth/refresh-token", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Response DTO for deserialization
    private record RefreshTokenResponse(
        string AccessToken,
        int ExpiresIn,
        string TokenType
    );
}
