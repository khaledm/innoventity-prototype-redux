using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Innoventity.API.Domain.Entities;
using Innoventity.API.Infrastructure.Authentication;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Innoventity.API.Features.Authentication;

public static class Register
{
    /// <summary>
    /// Registers a new actor account in the platform
    /// </summary>
    /// <remarks>
    /// Creates a new user account with PendingActivation status. An activation token is generated and must be
    /// verified before the account becomes active.
    ///
    /// **Validation Rules:**
    /// - **firstName**: Minimum 2 characters, maximum 50 characters (required)
    /// - **lastName**: Minimum 2 characters, maximum 50 characters (required)
    /// - **email**: Valid email format, unique per actor type (required)
    /// - **password**: Must meet complexity requirements (8+ chars, uppercase, lowercase, digit, special character)
    /// - **actorType**: Must be valid ActorType enum value (required)
    /// - **contactAddress**: Optional, but if provided, all fields (address1, city, postCode, countryCode) are required
    /// - **countryCode**: ISO 3166-1 alpha-2 format - exactly 2 uppercase letters (e.g., "US", "GB", "CA")
    /// - **phone**: Optional, maximum 20 characters
    ///
    /// **Breaking Changes (Phase 0.5):**
    /// - Registration now requires separate firstName and lastName (previously combined fullName)
    /// - ContactAddress is now a structured object (previously unstructured string)
    ///
    /// **Example Validation Errors:**
    ///
    /// Short firstName (400 Bad Request):
    /// ```json
    /// {
    ///   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    ///   "title": "Validation Failed",
    ///   "status": 400,
    ///   "detail": "First name must be at least 2 characters."
    /// }
    /// ```
    ///
    /// Invalid country code (400 Bad Request):
    /// ```json
    /// {
    ///   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    ///   "title": "Validation Failed",
    ///   "status": 400,
    ///   "detail": "Country code must be 2 uppercase letters (ISO 3166-1 alpha-2)."
    /// }
    /// ```
    ///
    /// Partial address (400 Bad Request):
    /// ```json
    /// {
    ///   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    ///   "title": "Validation Failed",
    ///   "status": 400,
    ///   "detail": "If address is provided, address1, city, postCode, and countryCode are all required."
    /// }
    /// ```
    /// </remarks>
    /// <response code="201">Actor registered successfully. Returns actor details and activation token.</response>
    /// <response code="400">Validation failed or email already registered for this actor type.</response>
    public static void MapRegisterEndpoint(this WebApplication app)
    {
        app.MapPost("/auth/register", async (
            RegisterRequest request,
            AppDbContext dbContext,
            PasswordHasher passwordHasher) =>
        {
            // Validate firstName and lastName minimum length
            if (request.FirstName.Length < 2)
            {
                return Results.BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Validation Failed",
                    status = 400,
                    detail = "First name must be at least 2 characters."
                });
            }

            if (request.LastName.Length < 2)
            {
                return Results.BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Validation Failed",
                    status = 400,
                    detail = "Last name must be at least 2 characters."
                });
            }

            // Validate ContactAddress if present
            if (request.ContactAddress != null)
            {
                // Validate all-or-nothing (if any field provided, all required fields must be present)
                if (string.IsNullOrWhiteSpace(request.ContactAddress.Address1) ||
                    string.IsNullOrWhiteSpace(request.ContactAddress.City) ||
                    string.IsNullOrWhiteSpace(request.ContactAddress.PostCode) ||
                    string.IsNullOrWhiteSpace(request.ContactAddress.CountryCode))
                {
                    return Results.BadRequest(new
                    {
                        type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                        title = "Validation Failed",
                        status = 400,
                        detail = "If address is provided, Address1, City, PostCode, and CountryCode are required."
                    });
                }

                // Validate CountryCode format (ISO 3166-1 alpha-2: exactly 2 uppercase letters)
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.ContactAddress.CountryCode, "^[A-Z]{2}$"))
                {
                    return Results.BadRequest(new
                    {
                        type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                        title = "Validation Failed",
                        status = 400,
                        detail = "Country code must be 2 uppercase letters (ISO 3166-1 alpha-2)."
                    });
                }
            }

            // Validate password complexity (R8.4)
            var passwordValidation = ValidatePasswordComplexity(request.Password);
            if (!passwordValidation.IsValid)
            {
                return Results.BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Validation Failed",
                    status = 400,
                    detail = passwordValidation.ErrorMessage
                });
            }

            // Parse ActorType
            if (!Enum.TryParse<ActorType>(request.ActorType, ignoreCase: true, out var actorType))
            {
                return Results.BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Validation Failed",
                    status = 400,
                    detail = "Invalid actor type. Must be one of: IdeaGenerator, RD, Manufacturing, SalesMarketing, Investor"
                });
            }

            // Check for duplicate email+ActorType (R1.3)
            var existingActor = await dbContext.Actors
                .FirstOrDefaultAsync(a => a.Email == request.Email && a.ActorType == actorType);

            if (existingActor != null)
            {
                return Results.Conflict(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    title = "Duplicate Registration",
                    status = 409,
                    detail = "This email address is already registered. Please use a different email or log in to your existing account."
                });
            }

            // Create actor with pending activation (R1.1)
            // Generate password salt (R8.5)
            var saltBytes = new byte[32];
            RandomNumberGenerator.Fill(saltBytes);
            var passwordSalt = Convert.ToBase64String(saltBytes);

            var actor = new Actor(Guid.NewGuid())
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                ContactAddress = request.ContactAddress != null ? new Address
                {
                    Address1 = request.ContactAddress.Address1,
                    Address2 = request.ContactAddress.Address2,
                    City = request.ContactAddress.City,
                    PostCode = request.ContactAddress.PostCode,
                    CountryCode = request.ContactAddress.CountryCode
                } : null,
                Phone = request.Phone,
                ActorType = actorType,
                AccountStatus = AccountStatus.PendingActivation,
                PasswordHash = passwordHasher.HashPassword(request.Password),
                PasswordSalt = passwordSalt,
                ActivationToken = GenerateActivationToken(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            dbContext.Actors.Add(actor);
            await dbContext.SaveChangesAsync();

            // In production, send activation email here with actor.ActivationToken
            // For MVP/testing, we include the token in response (remove in production)

            return Results.Created($"/actors/{actor.Id}", new
            {
                actorId = actor.Id.ToString(),
                email = actor.Email,
                actorType = actor.ActorType.ToString(),
                accountStatus = actor.AccountStatus.ToString(),
                activationToken = actor.ActivationToken, // TODO: Remove in production - send via email service instead
                message = "Registration successful. Please check your email for the activation link."
            });
        })
        .WithName("Register")
        .WithTags("Authentication")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Register a new actor account";
            operation.Description = "Creates a new user account with PendingActivation status. Requires email, full name, contact address, actor type, and password.";
            return operation;
        });
    }

    /// <summary>
    /// Validates password meets R8.4 complexity requirements:
    /// - Minimum 8 characters
    /// - Maximum 128 characters
    /// - At least 1 uppercase letter
    /// - At least 1 lowercase letter
    /// - At least 1 digit
    /// - At least 1 special character
    /// </summary>
    private static (bool IsValid, string? ErrorMessage) ValidatePasswordComplexity(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return (false, "Password is required.");
        }

        if (password.Length < 8)
        {
            return (false, "Password must be at least 8 characters and include uppercase, lowercase, digit, and special character.");
        }

        if (password.Length > 128)
        {
            return (false, "Password must not exceed 128 characters.");
        }

        var hasUppercase = Regex.IsMatch(password, @"[A-Z]");
        var hasLowercase = Regex.IsMatch(password, @"[a-z]");
        var hasDigit = Regex.IsMatch(password, @"[0-9]");
        var hasSpecialChar = Regex.IsMatch(password, @"[!@#$%^&*()\-_=+\[\]{}|;:,.<>?]");

        if (!hasUppercase || !hasLowercase || !hasDigit || !hasSpecialChar)
        {
            return (false, "Password must be at least 8 characters and include uppercase, lowercase, digit, and special character.");
        }

        return (true, null);
    }

    /// <summary>
    /// Generates a cryptographically secure activation token (T026)
    /// </summary>
    private static string GenerateActivationToken()
    {
        var tokenBytes = new byte[32];
        RandomNumberGenerator.Fill(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }

    /// <summary>
    /// Request model for actor registration
    /// </summary>
    /// <remarks>
    /// Contains all required information to create a new actor account.
    /// Note: DataAnnotations attributes are for documentation only - validation is performed manually in the endpoint handler.
    /// </remarks>
    public record RegisterRequest(
        /// <summary>
        /// Actor's email address (must be unique per actor type)
        /// </summary>
        /// <example>john.smith@example.com</example>
        [EmailAddress] string Email,

        /// <summary>
        /// Actor's first name (minimum 2 characters, maximum 50 characters)
        /// </summary>
        /// <example>John</example>
        [Required][MinLength(2)][MaxLength(50)] string FirstName,

        /// <summary>
        /// Actor's last name (minimum 2 characters, maximum 50 characters)
        /// </summary>
        /// <example>Smith</example>
        [Required][MinLength(2)][MaxLength(50)] string LastName,

        /// <summary>
        /// Actor's contact address (optional, but if provided, all fields are required)
        /// </summary>
        AddressRequest? ContactAddress,

        /// <summary>
        /// Actor's phone number (optional, maximum 20 characters)
        /// </summary>
        /// <example>+1-555-123-4567</example>
        string? Phone,

        /// <summary>
        /// Type of actor (e.g., "IdeaGenerator", "Manufacturing", "SalesMarketing", "Engineering", "Investor")
        /// </summary>
        /// <example>IdeaGenerator</example>
        [Required] string ActorType,

        /// <summary>
        /// Account password (minimum 8 characters, must include uppercase, lowercase, digit, and special character)
        /// </summary>
        [Required] string Password
    );

    /// <summary>
    /// Structured address information (value object)
    /// </summary>
    /// <remarks>
    /// All-or-nothing validation: If any address field is provided, all required fields (address1, city, postCode, countryCode) must be provided.
    /// Address is stored as an owned entity in the database (no separate AddressId).
    /// </remarks>
    public record AddressRequest(
        /// <summary>
        /// Primary address line (required if address provided)
        /// </summary>
        /// <example>123 Main Street</example>
        [Required] string Address1,

        /// <summary>
        /// Secondary address line (optional)
        /// </summary>
        /// <example>Apartment 4B</example>
        string? Address2,

        /// <summary>
        /// City name (required if address provided)
        /// </summary>
        /// <example>London</example>
        [Required] string City,

        /// <summary>
        /// Postal/ZIP code (required if address provided)
        /// </summary>
        /// <example>SW1A 1AA</example>
        [Required] string PostCode,

        /// <summary>
        /// ISO 3166-1 alpha-2 country code - exactly 2 uppercase letters (required if address provided)
        /// </summary>
        /// <example>GB</example>
        /// <remarks>
        /// Common codes: US (United States), GB (United Kingdom), CA (Canada), AU (Australia), DE (Germany), FR (France), JP (Japan)
        /// Full list: https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2
        /// </remarks>
        [Required][StringLength(2, MinimumLength = 2)][RegularExpression("^[A-Z]{2}$", ErrorMessage = "Country code must be 2 uppercase letters (ISO 3166-1 alpha-2)")] string CountryCode
    );
}
