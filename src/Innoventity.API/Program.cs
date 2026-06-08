using System.Reflection;
using System.Text;
using Innoventity.API.Features.Authentication;
using Innoventity.API.Features.Bids;
using Innoventity.API.Features.Health;
using Innoventity.API.Features.Industries;
using Innoventity.API.Features.Innovations;
using Innoventity.API.Infrastructure.Authentication;
using Innoventity.API.Infrastructure.ErrorHandling;
using Innoventity.API.Infrastructure.Logging;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// App Service Linux warmup probes require the app to listen on the assigned PORT
// and all interfaces. Keep local/default hosting behavior when ASPNETCORE_URLS is set.
var appServicePort = Environment.GetEnvironmentVariable("PORT");
var configuredUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (!string.IsNullOrWhiteSpace(appServicePort) && string.IsNullOrWhiteSpace(configuredUrls))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{appServicePort}");
}

// Configure Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Configure CORS policy (T061 / CHK092)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowCredentials()
                  .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                  .WithHeaders("Content-Type", "Authorization");
        }
        else
        {
            // No origins configured — block all cross-origin requests
            policy.SetIsOriginAllowed(_ => false);
        }
    });
});

// Configure OpenAPI documentation (T060)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Innoventity Platform API",
        Version = "v1",
        Description = "Open Innovation Platform - Phase 0.6 MVP\n\n" +
                      "**Authentication**: Use POST /auth/login to obtain a JWT bearer token, " +
                      "then click 'Authorize' and enter: Bearer {your-token}"
    });

    // Include XML documentation comments in Swagger UI
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // JWT Bearer security definition - enables 'Authorize' button in Swagger UI
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token. Obtain it via POST /auth/login.\n\nExample: eyJhbGciOiJIUzI1NiIs..."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.MigrationsAssembly("Innoventity.API")));

// Configure JWT Authentication (defensive - allow startup even if misconfigured for health checks)
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];

if (!string.IsNullOrEmpty(jwtSigningKey))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey))
            };
        });
}
else
{
    // Register authentication services without JWT bearer for health check validation
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization();

// Register services
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<ICurrentActorService, CurrentActorService>();

var app = builder.Build();

// Configure error handling
app.ConfigureExceptionHandler();

// Propagate / generate X-Correlation-ID for every request (T062 / FR7.6)
app.UseCorrelationId();

// Configure Swagger and Scalar API documentation (T060) - Development only
if (app.Environment.IsDevelopment())
{
    app.MapSwagger("/openapi/{documentName}.json");
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Innoventity Platform API")
               .WithTheme(ScalarTheme.Purple);
    });
    // Swagger UI at /swagger/index.html (alternative to Scalar)
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Innoventity API v1");
        options.RoutePrefix = "swagger";
    });
    app.UseSwagger();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapHealthEndpoint();
app.MapRegisterEndpoint();
app.MapActivateEndpoint();
app.MapLoginEndpoint();
app.MapRefreshTokenEndpoint();
app.MapGetInnovation();
app.MapCreateInnovation(); // T005: POST /innovations - Create draft innovation
app.MapUpdateInnovation(); // T006: PUT /innovations/{id} - Update draft innovation
app.MapSubmitInnovation(); // T007: PATCH /innovations/{id}/submit - Publish innovation
app.MapListInnovations(); // T008: GET /innovations - List published innovations
app.MapGetIndustries(); // T009: GET /industries - Industry master list (public, no auth)
app.MapSubmitBid(); // T010: POST /innovations/{innovationId}/bids - Submit partnership proposal
app.MapGetBids(); // T011: GET /innovations/{innovationId}/bids - List bids for innovation (owner only)
app.MapUpdateBid(); // T012: PUT /bids/{bidId} - Update unaccepted bid (author only)
app.MapSelectPartners(); // T094: POST /innovations/{innovationId}/select-partners - Complete partner selection
app.MapGet("/", () => "Hello World!");

app.Run();
