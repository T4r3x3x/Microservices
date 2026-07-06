using Orders.Api.Features.Carts;
using Orders.Api.Features.Orders;
using Orders.Api.Infrastructure.Messaging;
using Orders.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

bool ordersDatabaseConfigured =
    builder.Configuration.GetConnectionString("orders-db") is not null;
bool messagingConfigured =
    builder.Configuration.GetConnectionString("messaging") is not null;

if (ordersDatabaseConfigured)
{
    builder.AddNpgsqlDbContext<OrdersDbContext>("orders-db");
}

if (messagingConfigured)
{
    builder.AddRabbitMQClient("messaging");
}

if (ordersDatabaseConfigured && messagingConfigured)
{
    builder.Services.AddHostedService<OutboxPublisherWorker>();
}

builder.Services.AddHttpClient<CartClient>("cart-api", static client =>
{
    client.BaseAddress = new Uri("https+http://cart-api");
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
    Service = "Orders.Api",
    Status = "ok"
}));

if (ordersDatabaseConfigured)
{
    app.MapOrderEndpoints();
}

app.MapDefaultEndpoints();

app.Run();

public partial class Program;
