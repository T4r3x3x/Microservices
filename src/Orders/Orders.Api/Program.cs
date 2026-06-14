var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

if (builder.Configuration.GetConnectionString("orders-db") is not null)
{
    builder.AddNpgsqlDataSource("orders-db");
}

if (builder.Configuration.GetConnectionString("messaging") is not null)
{
    builder.AddRabbitMQClient("messaging");
}

builder.Services.AddHttpClient("cart-api", static client =>
{
    client.BaseAddress = new Uri("https+http://cart-api");
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
    Service = "Orders.Api",
    Status = "ok"
}));

app.MapDefaultEndpoints();

app.Run();

public partial class Program;
