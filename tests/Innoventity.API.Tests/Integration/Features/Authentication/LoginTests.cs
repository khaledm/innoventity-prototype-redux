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

        var actor = new Actor(Guid.NewGuid())
        {
            Email = "login.test@example.com",
            FirstName = "Login",
            LastName = "Test User",
            ContactAddress = new Address
            {
                Address1 = "123 Test St",
                City = "Test City",
                PostCode = "12345",
                CountryCode = "US"
            },
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword("ValidPass123!"),
            PasswordSalt = "somesalt",
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
        Assert.Equal("Login", loginResponse.Actor.FirstName);
        Assert.Equal("Test User", loginResponse.Actor.LastName);
        Assert.Equal("Login Test User", loginResponse.Actor.DisplayName);
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

        var actor = new Actor(Guid.NewGuid())
        {
            Email = "pending@example.com",
            FirstName = "Pending",
            LastName = "User",
            ContactAddress = new Address
            {
                Address1 = "123 Test St",
                City = "Test City",
                PostCode = "12345",
                CountryCode = "US"
            },
            ActorType = ActorType.RD,
            AccountStatus = AccountStatus.PendingActivation,
            PasswordHash = passwordHasher.HashPassword("ValidPass123!"),
            PasswordSalt = "somesalt",
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
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Please activate your account using the link sent to your email", body);
        Assert.Contains("Account Not Activated", body);
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

        var actor = new Actor(Guid.NewGuid())
        {
            Email = "wrongpass@example.com",
            FirstName = "Wrong",
            LastName = "Pass User",
            ContactAddress = new Address
            {
                Address1 = "123 Test St",
                City = "Test City",
                PostCode = "12345",
                CountryCode = "US"
            },
            ActorType = ActorType.Manufacturing,
            AccountStatus = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword("CorrectPass123!"),
            PasswordSalt = "somesalt",
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
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email, actor type, or password", body);
        Assert.Contains("Authentication Failed", body);
    }

    // T082: Login lockout integration tests (R8.4)

    [Fact]
    public async Task Login_AfterFiveFailedAttempts_Returns423Locked_WithLockoutMessage_R8_4()
    {
        // Arrange
        var factory = CreateFactory();
        var client = factory.CreateClient();

        using var seedScope = factory.Services.CreateScope();
        var seedContext = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = seedScope.ServiceProvider.GetRequiredService<PasswordHasher>();

        var actor = new Actor(Guid.NewGuid())
        {
            Email = "lockout423@example.com",
            FirstName = "Lockout",
            LastName = "Test",
            ContactAddress = new Address
            {
                Address1 = "123 Lock St",
                City = "Lockout City",
                PostCode = "00000",
                CountryCode = "US"
            },
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword("CorrectPass123!"),
            PasswordSalt = "somesalt"
        };

        seedContext.Actors.Add(actor);
        await seedContext.SaveChangesAsync();

        var failRequest = new
        {
            email = "lockout423@example.com",
            actorType = "IdeaGenerator",
            password = "WRONG_PASSWORD!"
        };

        // Act: 5 consecutive wrong-password attempts
        for (int i = 0; i < 5; i++)
            await client.PostAsJsonAsync("/auth/login", failRequest);

        // 6th attempt while locked
        var lockedResponse = await client.PostAsJsonAsync("/auth/login", failRequest);

        // Assert: HTTP 423 Locked
        Assert.Equal(System.Net.HttpStatusCode.Locked, lockedResponse.StatusCode);

        // Assert: RFC 7807 response body contains the exact lockout message and title
        var responseBody = await lockedResponse.Content.ReadAsStringAsync();
        Assert.Contains(
            "Too many failed login attempts. Account temporarily locked for 15 minutes.",
            responseBody);
        Assert.Contains("Locked", responseBody);
    }

    [Fact]
    public async Task Login_AfterFiveFailedAttempts_PersistsLockoutUntilAnd5FailedCount_R8_4()
    {
        // Arrange
        var factory = CreateFactory();
        var client = factory.CreateClient();

        var actorId = Guid.NewGuid();

        using var seedScope = factory.Services.CreateScope();
        var seedContext = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = seedScope.ServiceProvider.GetRequiredService<PasswordHasher>();

        var actor = new Actor(actorId)
        {
            Email = "lockoutdb@example.com",
            FirstName = "Lockout",
            LastName = "Db",
            ContactAddress = new Address
            {
                Address1 = "123 Lock St",
                City = "Lockout City",
                PostCode = "00000",
                CountryCode = "US"
            },
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword("CorrectPass123!"),
            PasswordSalt = "somesalt"
        };

        seedContext.Actors.Add(actor);
        await seedContext.SaveChangesAsync();

        var failRequest = new
        {
            email = "lockoutdb@example.com",
            actorType = "IdeaGenerator",
            password = "WRONG_PASSWORD!"
        };

        var before = DateTimeOffset.UtcNow;

        // Act: 5 consecutive wrong-password attempts
        for (int i = 0; i < 5; i++)
            await client.PostAsJsonAsync("/auth/login", failRequest);

        var after = DateTimeOffset.UtcNow;

        // Read back from DB via a fresh scope (reads from shared in-memory store)
        using var readScope = factory.Services.CreateScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updatedActor = await readContext.Actors.FindAsync(actorId);

        // Assert: FailedLoginAttempts = 5 and LockoutUntil is ~15 min in future
        Assert.NotNull(updatedActor);
        Assert.Equal(5, updatedActor.FailedLoginAttempts);
        Assert.NotNull(updatedActor.LockoutUntil);
        Assert.InRange(
            updatedActor.LockoutUntil!.Value,
            before.AddMinutes(14).AddSeconds(55),
            after.AddMinutes(15).AddSeconds(5));
    }

    [Fact]
    public async Task Login_WithCorrectPassword_AfterPriorFailures_ResetsFailedCount_R8_4()
    {
        // Arrange
        var factory = CreateFactory();
        var client = factory.CreateClient();

        var actorId = Guid.NewGuid();

        using var seedScope = factory.Services.CreateScope();
        var seedContext = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = seedScope.ServiceProvider.GetRequiredService<PasswordHasher>();

        var actor = new Actor(actorId)
        {
            Email = "lockoutreset@example.com",
            FirstName = "Lockout",
            LastName = "Reset",
            ContactAddress = new Address
            {
                Address1 = "123 Lock St",
                City = "Lockout City",
                PostCode = "00000",
                CountryCode = "US"
            },
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword("CorrectPass123!"),
            PasswordSalt = "somesalt"
        };

        seedContext.Actors.Add(actor);
        await seedContext.SaveChangesAsync();

        // Act: 3 wrong-password attempts then 1 correct
        var failRequest = new { email = "lockoutreset@example.com", actorType = "IdeaGenerator", password = "WRONG!" };
        var successRequest = new { email = "lockoutreset@example.com", actorType = "IdeaGenerator", password = "CorrectPass123!" };

        for (int i = 0; i < 3; i++)
            await client.PostAsJsonAsync("/auth/login", failRequest);

        var successResponse = await client.PostAsJsonAsync("/auth/login", successRequest);
        Assert.Equal(System.Net.HttpStatusCode.OK, successResponse.StatusCode);

        // Read back from DB
        using var readScope = factory.Services.CreateScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updatedActor = await readContext.Actors.FindAsync(actorId);

        // Assert: counter reset to 0 after successful login
        Assert.NotNull(updatedActor);
        Assert.Equal(0, updatedActor.FailedLoginAttempts);
        Assert.Null(updatedActor.LockoutUntil);
    }

    [Fact]
    public async Task Login_WithInvalidActorTypeString_ShouldReturn400_WithInvalidActorTypeMessage()
    {
        // Covers the path where Enum.TryParse fails (NoCoverage: Login.cs detail "Invalid actor type")
        var factory = CreateFactory();
        var client = factory.CreateClient();

        var request = new
        {
            email = "any@example.com",
            actorType = "NotARealActorType",
            password = "AnyPassword!"
        };

        var response = await client.PostAsJsonAsync("/auth/login", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid actor type", body);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ShouldReturn401_WithErrorMessage()
    {
        // Covers the actor-not-found path (NoCoverage: Login.cs detail/title on null actor)
        var factory = CreateFactory();
        var client = factory.CreateClient();

        var request = new
        {
            email = "nobody@example.com",
            actorType = "IdeaGenerator",
            password = "AnyPassword!"
        };

        var response = await client.PostAsJsonAsync("/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email, actor type, or password", body);
        Assert.Contains("Authentication Failed", body);
    }

    [Fact]
    public async Task Login_WithLowercaseActorType_ShouldSucceed()
    {
        // Kills the ignoreCase:true → false Boolean mutation (Login.cs line 44)
        var factory = CreateFactory();
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();

        var actor = new Actor(Guid.NewGuid())
        {
            Email = "lowercase.type@example.com",
            FirstName = "Lower",
            LastName = "Case",
            ContactAddress = new Address
            {
                Address1 = "1 Test St",
                City = "Testville",
                PostCode = "00001",
                CountryCode = "US"
            },
            ActorType = ActorType.IdeaGenerator,
            AccountStatus = AccountStatus.Active,
            PasswordHash = passwordHasher.HashPassword("LowerPass123!"),
            PasswordSalt = "somesalt"
        };

        dbContext.Actors.Add(actor);
        await dbContext.SaveChangesAsync();

        // Send actor type in all-lowercase — must still succeed with ignoreCase: true
        var request = new
        {
            email = "lowercase.type@example.com",
            actorType = "ideagenerator",
            password = "LowerPass123!"
        };

        var response = await client.PostAsJsonAsync("/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
        string FirstName,
        string LastName,
        string DisplayName,
        string ActorType
    );
}
