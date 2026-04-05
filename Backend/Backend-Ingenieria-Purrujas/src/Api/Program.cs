using Backend_Ingenieria_Purrujas.Application.Quotes;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Dependency Injection
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IRoomTypeRepository, RoomTypeRepository>();
builder.Services.AddScoped<ISeasonRepository, SeasonRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
