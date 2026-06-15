using Catalog.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

if (builder.Configuration.GetConnectionString("catalog-db") is not null)
{
    builder.AddNpgsqlDbContext<CatalogDbContext>("catalog-db");
}

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

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

app.MapDefaultEndpoints();

app.Run();

public partial class Program;
