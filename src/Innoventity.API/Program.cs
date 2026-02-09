using System.Text;
using Innoventity.API.Features.Health;
using Innoventity.API.Infrastructure.Authentication;
using Innoventity.API.Infrastructure.ErrorHandling;
using Innoventity.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configure Application Insights
builder.Services.AddApplicationInsightsTelemetry();

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

app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapHealthEndpoint();
app.MapGet("/", () => "Hello World!");

app.Run();
