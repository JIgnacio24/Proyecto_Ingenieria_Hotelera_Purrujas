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
var allowedOriginsSet = new HashSet<string>(allowedOrigins, StringComparer.OrdinalIgnoreCase);
var allowLocalDevelopmentOrigins = builder.Environment.IsDevelopment();

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
        policy.SetIsOriginAllowed(origin =>
                  allowedOriginsSet.Contains(origin)
                  || (allowLocalDevelopmentOrigins && IsLocalDevelopmentOrigin(origin)))
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Dependency Injection
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IFacilitiesPageContentRepository, FacilitiesPageContentRepository>();
builder.Services.AddScoped<IRoomTypeRepository, RoomTypeRepository>();
builder.Services.AddScoped<ISeasonRepository, SeasonRepository>();

var app = builder.Build();
var configuredUrls = builder.Configuration["ASPNETCORE_URLS"] ?? string.Empty;
var hasHttpsEndpoint = configuredUrls
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.OnStarting(static state =>
        {
            var response = (HttpResponse)state;
            response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
            response.Headers.Pragma = "no-cache";
            response.Headers.Expires = "0";
            return Task.CompletedTask;
        }, context.Response);
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

if (hasHttpsEndpoint)
{
    app.UseHttpsRedirection();
}

app.UseCors("ClientApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static bool IsLocalDevelopmentOrigin(string origin)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
    {
        return false;
    }

    var isHttp = uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    var isLocalHost = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);

    return isHttp && isLocalHost;
}
