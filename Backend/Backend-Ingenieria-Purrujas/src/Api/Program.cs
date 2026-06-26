using Backend_Ingenieria_Purrujas.Application.Auth;
using Backend_Ingenieria_Purrujas.Application.AdminAudit;
using Backend_Ingenieria_Purrujas.Application.Email;
using Backend_Ingenieria_Purrujas.Application.Occupancy;
using Backend_Ingenieria_Purrujas.Application.Quotes;
using Backend_Ingenieria_Purrujas.Application.Reservations;
using Backend_Ingenieria_Purrujas.Api.Services;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Backend_Ingenieria_Purrujas.Infrastructure.Data;
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
    "http://localhost:4203",
    "https://localhost:4203",
    "http://localhost:4204",
    "https://localhost:4204",
    "http://127.0.0.1:4203",
    "http://127.0.0.1:4204"
];
var allowedOriginsSet = new HashSet<string>(allowedOrigins, StringComparer.OrdinalIgnoreCase);
var allowLocalDevelopmentOrigins = builder.Environment.IsDevelopment();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("No se configuró Jwt:Key.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Backend-Ingenieria-Purrujas";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Frontend-Ingenieria-Purrujas-Admin";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
// Caché en memoria para datos de referencia (temporadas, promociones, tipos de
// habitación) usados por la Cotización Rápida. Evita reconsultar en cada recálculo.
builder.Services.AddMemoryCache();
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
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminAuditLogService, AdminAuditLogService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IAdminAuditLogRepository, AdminAuditLogRepository>();
builder.Services.AddScoped<IFacilitiesPageContentRepository, FacilitiesPageContentRepository>();
// Repositorio del contenido editable de la pagina publica "Sobre Nosotros".
builder.Services.AddScoped<IAboutUsPageContentRepository, AboutUsPageContentRepository>();
builder.Services.AddScoped<IHomePageContentRepository, HomePageContentRepository>();
builder.Services.AddScoped<IGettingTherePageContentRepository, GettingTherePageContentRepository>();
builder.Services.AddScoped<IRoomTypeRepository, RoomTypeRepository>();
builder.Services.AddScoped<ISeasonRepository, SeasonRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IAdvertisingRepository, AdvertisingRepository>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IGalleryImagesRepository, GalleryImagesRepository>();
builder.Services.AddScoped<IRoomAvailabilityRepository, RoomAvailabilityRepository>();
builder.Services.AddScoped<RoomAvailabilityPdfService>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IOccupancyRepository, OccupancyRepository>();
builder.Services.AddScoped<IOccupancyPredictionService, OccupancyPredictionService>();
builder.Services.AddScoped<OccupancySeeder>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<OccupancySeeder>();
    await seeder.SeedAsync();
}
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
app.UseStaticFiles(); //Usar imagenes
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
