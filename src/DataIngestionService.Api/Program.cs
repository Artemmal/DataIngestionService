using DataIngestionService.Api.Persistence;
using DataIngestionService.Api.Services;
using DataIngestionService.Api.Services.Abstractions;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<DeduplicationService>();
builder.Services.AddScoped<ITransactionIngestionService, TransactionIngestionService>();
builder.Services.AddScoped<ITransactionQueryService, TransactionQueryService>();
builder.Services.AddScoped<IStatsService, StatsService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

public partial class Program;