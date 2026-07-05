using Cart.Api.Features.Carts;
using Cart.Api.Features.Catalog;
using Cart.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddOptions<CartOptions>()
    .BindConfiguration(CartOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

if (builder.Configuration.GetConnectionString("cart-cache") is not null)
{
    builder.AddRedisClient("cart-cache");
    builder.Services.AddScoped<ICartStore, RedisCartStore>();
}

builder.Services.AddHttpClient<ProductCatalogClient>("catalog-api", static client =>
{
    client.BaseAddress = new Uri("https+http://catalog-api");
});

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new
{
    Service = "Cart.Api",
    Status = "ok"
}));

if (builder.Configuration.GetConnectionString("cart-cache") is not null)
{
    app.MapCartEndpoints();
}

app.MapDefaultEndpoints();

app.Run();

public partial class Program;
