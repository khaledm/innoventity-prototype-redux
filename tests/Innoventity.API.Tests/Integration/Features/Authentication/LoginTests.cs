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

public class LoginTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factoryBase;

    public LoginTests(WebApplicationFactory<Program> factory)
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

                var dbName = $"LoginTests_{Guid.NewGuid()}";
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
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange: Create factory and client
        var factory = CreateFactory();
        var client = factory.CreateClient();

        // Seed an activated account
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();

        var actor = new Actor
        {
            Id = Guid.NewGuid(),
            Email = "login.test@example.com",
            FullName = "Login Test User",
            ContactAddress = "123 Test St",
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword("ValidPass123!"),
            ActivationToken = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Actors.Add(actor);
        await dbContext.SaveChangesAsync();

        var loginRequest = new
        {
            email = "login.test@example.com",
            actorType = "IdeaGenerator",
            password = "ValidPass123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);
        Assert.NotNull(loginResponse.AccessToken);
        Assert.NotNull(loginResponse.RefreshToken);
        Assert.Equal(3600, loginResponse.ExpiresIn);
        Assert.Equal("Bearer", loginResponse.TokenType);
        Assert.Equal(actor.Id, loginResponse.Actor.ActorId);
        Assert.Equal("login.test@example.com", loginResponse.Actor.Email);
        Assert.Equal("Login Test User", loginResponse.Actor.FullName);
        Assert.Equal("IdeaGenerator", loginResponse.Actor.ActorType);
    }

    [Fact]
    public async Task Login_WithPendingActivationAccount_ShouldReturn401()
    {
        // Arrange: Create factory and client
        var factory = CreateFactory();
        var client = factory.CreateClient();

        // Seed a pending activation account
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();

        var actor = new Actor
        {
            Id = Guid.NewGuid(),
            Email = "pending@example.com",
            FullName = "Pending User",
            ContactAddress = "123 Test St",
            ActorType = ActorType.RD,
            AccountStatus = AccountStatus.PendingActivation,
            PasswordHash = passwordHasher.HashPassword("ValidPass123!"),
            ActivationToken = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Actors.Add(actor);
        await dbContext.SaveChangesAsync();

        var loginRequest = new
        {
            email = "pending@example.com",
            actorType = "RD",
            password = "ValidPass123!"
        };

       // Act
        var response = await client.PostAsJsonAsync("/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturn401()
    {
        // Arrange: Create factory and client
        var factory = CreateFactory();
        var client = factory.CreateClient();

        // Seed an activated account
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();

        var actor = new Actor
        {
            Id = Guid.NewGuid(),
            Email = "wrongpass@example.com",
            FullName = "Wrong Pass User",
            ContactAddress = "123 Test St",
            ActorType = ActorType.Manufacturing,
            AccountStatus = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword("CorrectPass123!"),
            ActivationToken = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Actors.Add(actor);
        await dbContext.SaveChangesAsync();

        var loginRequest = new
        {
            email = "wrongpass@example.com",
            actorType = "Manufacturing",
            password = "WrongPass123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Response DTOs for deserialization
    private record LoginResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        string TokenType,
        ActorInfo Actor
    );

    private record ActorInfo(
        Guid ActorId,
        string Email,
        string FullName,
        string ActorType
    );
}
