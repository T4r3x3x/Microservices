using Catalog.Api.Features.Products;
using Catalog.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

bool catalogDatabaseConfigured =
    builder.Configuration.GetConnectionString("catalog-db") is not null;

if (catalogDatabaseConfigured)
{
    builder.AddNpgsqlDbContext<CatalogDbContext>("catalog-db");
}

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

if (app.Environment.IsDevelopment() && catalogDatabaseConfigured)
{
    await app.Services.SeedCatalogAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new
{
    Service = "Catalog.Api",
    Status = "ok"
}));

if (catalogDatabaseConfigured)
{
    app.MapProductEndpoints();
}

app.MapDefaultEndpoints();

app.Run();

public partial class Program;
