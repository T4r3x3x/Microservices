var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

if (builder.Configuration.GetConnectionString("cart-cache") is not null)
{
    builder.AddRedisClient("cart-cache");
}

builder.Services.AddHttpClient("catalog-api", static client =>
{
    client.BaseAddress = new Uri("https+http://catalog-api");
});

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
    Service = "Cart.Api",
    Status = "ok"
}));

app.MapDefaultEndpoints();

app.Run();

public partial class Program;
