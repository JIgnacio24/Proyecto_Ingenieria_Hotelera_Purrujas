using Backend_Ingenieria_Purrujas.Application.Auth;
using Backend_Ingenieria_Purrujas.Application.Quotes;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Backend_Ingenieria_Purrujas.Infrastructure.Repositories;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
// Load .env first and then the environment-specific file if it exists.
var environmentFile = $".env.{builder.Environment.EnvironmentName.ToLowerInvariant()}";
var envFiles = new[] { ".env", environmentFile }
    .Where(File.Exists)
    .ToArray();

if (envFiles.Length > 0)
{
    foreach (var file in envFiles)
    {
        DotNetEnv.Env.Load(file);
    }
}

builder.Configuration.AddEnvironmentVariables();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
[
    "http://localhost:4200",
    "https://localhost:4200",
    "http://localhost:4201",
    "https://localhost:4201",
    "http://127.0.0.1:4200",
    "http://127.0.0.1:4201"
];

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("No se configuro Jwt:Key.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Backend-Ingenieria-Purrujas";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Frontend-Ingenieria-Purrujas-Admin";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Administrador");
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Dependency Injection
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IRoomTypeRepository, RoomTypeRepository>();
builder.Services.AddScoped<ISeasonRepository, SeasonRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var httpsEnabled = app.Urls.Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) ||
                   builder.Configuration["ASPNETCORE_URLS"]?.Contains("https://", StringComparison.OrdinalIgnoreCase) == true;

if (httpsEnabled)
{
    app.UseHttpsRedirection();
}

app.UseCors("ClientApp");
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers.Expires = "0";
            return Task.CompletedTask;
        });
    }

    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
