using System.Text;
using Innoventity.API.Features.Authentication;
using Innoventity.API.Features.Bids;
using Innoventity.API.Features.Health;
using Innoventity.API.Features.Industries;
using Innoventity.API.Features.Innovations;
using Innoventity.API.Infrastructure.Authentication;
using Innoventity.API.Infrastructure.ErrorHandling;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Configure OpenAPI documentation (T060)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Innoventity Platform API",
        Version = "v1",
        Description = "Open Innovation Platform - Phase 0 MVP"
    });
});

// Configure Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

var app = builder.Build();

// Configure error handling
app.ConfigureExceptionHandler();

// Configure Swagger and Scalar API documentation (T060) - Development only
if (app.Environment.IsDevelopment())
{
    app.MapSwagger("/openapi/{documentName}.json");
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Innoventity Platform API")
               .WithTheme(ScalarTheme.Purple);
    });
}

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
app.MapGet("/", () => "Hello World!");

app.Run();
